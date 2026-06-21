using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Eskiz.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Eskiz;

// Eskiz.uz SMS shlyuzi. Token JWT — ~30 kun yashaydi, /auth/refresh (PATCH) bilan yangilanadi.
// Singleton: token in-memory cache'da saqlanadi (BtsBroker bilan bir xil yondashuv).
public class EskizBroker(
    IHttpClientFactory httpClientFactory,
    IOptions<EskizOptions> options,
    ILogger<EskizBroker> logger) : IEskizBroker
{
    private readonly EskizOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    #region Auth

    public async Task<Result> LoginAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateHttpClient();

            using var content = Form(
                ("email", _options.Email),
                ("password", _options.Password));

            var response = await client.PostAsync("/api/auth/login", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Eskiz: login failed with {StatusCode}", response.StatusCode);
                return EskizErrors.LoginFailed;
            }

            var result = await response.Content.ReadFromJsonAsync<EskizAuthResponse>(cancellationToken);
            if (string.IsNullOrEmpty(result?.Data?.Token))
                return EskizErrors.TokenEmpty;

            _token = result.Data.Token;
            _tokenExpiry = DateTime.UtcNow.AddDays(29); // token ~30 kun; ehtiyot uchun 29 kunda yangilaymiz
            logger.LogInformation("Eskiz: logged in successfully");
            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Tarmoq/DNS uzilishi — exception tashlamaymiz, Result.Failure qaytaramiz.
            logger.LogWarning(ex, "Eskiz: login network error");
            return EskizErrors.LoginFailed;
        }
    }

    private async Task<Result> RefreshAsync(CancellationToken cancellationToken)
    {
        if (_token is null) return EskizErrors.RefreshFailed;

        try
        {
            var client = CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/auth/refresh");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _token = null; // refresh bo'lmadi — keyingi safar qaytadan login qilamiz
                return EskizErrors.RefreshFailed;
            }

            var result = await response.Content.ReadFromJsonAsync<EskizAuthResponse>(cancellationToken);
            if (string.IsNullOrEmpty(result?.Data?.Token))
                return EskizErrors.TokenEmpty;

            _token = result.Data.Token;
            _tokenExpiry = DateTime.UtcNow.AddDays(29);
            logger.LogInformation("Eskiz: token refreshed");
            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Eskiz: refresh network error");
            return EskizErrors.RefreshFailed;
        }
    }

    // Yaroqli token qaytaradi: cache → refresh → login fallback. Thread-safe.
    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _token is not null && DateTime.UtcNow < _tokenExpiry)
                return _token;

            if (_token is not null)
            {
                var refresh = await RefreshAsync(cancellationToken);
                if (refresh.IsSuccess) return _token;
            }

            var login = await LoginAsync(cancellationToken);
            return login.IsSuccess ? _token : null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    #endregion

    #region Sending

    public async Task<Result<EskizSendResponse>> SendSmsAsync(string mobilePhone, string message,
        string? callbackUrl = null, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (token is null) return EskizErrors.LoginFailed;

        var result = await SendFormAsync<EskizSendResponse>("/api/message/sms/send", token, cancellationToken,
            ("mobile_phone", mobilePhone),
            ("message", message),
            ("from", _options.From),
            ("callback_url", string.IsNullOrWhiteSpace(callbackUrl) ? _options.CallbackUrl : callbackUrl));

        if (result is null)
            return EskizErrors.SendFailed;

        // Eskiz xato holatda HAM id qaytaradi — muvaffaqiyat faqat status == "waiting" bo'lganda.
        if (!string.Equals(result.Status, "waiting", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Eskiz send rejected (status={Status}): {Message}", result.Status, result.Message);
            return EskizErrors.Rejected(result.Message);
        }

        if (string.IsNullOrEmpty(result.Id))
            return EskizErrors.SendFailed;

        return result;
    }

    #endregion

    #region Balance

    public async Task<Result<decimal>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (token is null) return EskizErrors.LoginFailed;

        var result = await SendGetAsync<EskizBalanceResponse>("/api/user/get-limit", token, cancellationToken);
        if (result?.Data is null)
            return EskizErrors.GetBalanceFailed;

        return result.Data.Balance;
    }

    #endregion

    #region HTTP helpers

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient("Eskiz");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    // Eskiz login/send — multipart/form-data kutadi (BTS'dan farqi).
    private static MultipartFormDataContent Form(params (string Key, string? Value)[] fields)
    {
        var content = new MultipartFormDataContent();
        foreach (var (key, value) in fields)
            content.Add(new StringContent(value ?? string.Empty), key);
        return content;
    }

    private const int MaxAttempts = 4;

    private async Task<T?> SendFormAsync<T>(string path, string token, CancellationToken cancellationToken,
        params (string Key, string? Value)[] fields)
    {
        var authRetried = false;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var client = CreateHttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var content = Form(fields);
                var response = await client.PostAsync(path, content, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && !authRetried)
                {
                    authRetried = true;
                    var refreshed = await GetTokenAsync(cancellationToken, forceRefresh: true);
                    if (refreshed is null) return default;
                    token = refreshed;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("Eskiz POST {Path} failed with {StatusCode}: {Body}", path, response.StatusCode, body);
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            // Timeout / DNS / tarmoq uzilishi — abort qilmaymiz, qayta urinamiz (haqiqiy cancel bundan mustasno).
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException)
                                       && !cancellationToken.IsCancellationRequested)
            {
                if (attempt >= MaxAttempts)
                {
                    logger.LogWarning(ex, "Eskiz POST {Path} failed after {Attempts} attempts", path, attempt);
                    return default;
                }

                await Task.Delay(attempt * 500, cancellationToken);
            }
        }

        return default;
    }

    private async Task<T?> SendGetAsync<T>(string path, string token, CancellationToken cancellationToken)
    {
        var authRetried = false;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var client = CreateHttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync(path, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && !authRetried)
                {
                    authRetried = true;
                    var refreshed = await GetTokenAsync(cancellationToken, forceRefresh: true);
                    if (refreshed is null) return default;
                    token = refreshed;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Eskiz GET {Path} failed with {StatusCode}", path, response.StatusCode);
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException)
                                       && !cancellationToken.IsCancellationRequested)
            {
                if (attempt >= MaxAttempts)
                {
                    logger.LogWarning(ex, "Eskiz GET {Path} failed after {Attempts} attempts", path, attempt);
                    return default;
                }

                await Task.Delay(attempt * 500, cancellationToken);
            }
        }

        return default;
    }

    #endregion
}
