using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress;

public class BtsBroker(
    IHttpClientFactory httpClientFactory,
    IOptions<BtsExpressOptions> options,
    IMemoryCache cache,
    ILogger<BtsBroker> logger) : IBtsBroker
{
    private readonly BtsExpressOptions _options = options.Value;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _accessTokenExpiry = DateTime.MinValue;

    #region Auth

    public async Task<Result> LoginAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();

        var response = await client.PostAsJsonAsync("/auth/login", new BtsLoginRequest
        {
            Login = _options.Login,
            Password = _options.Password
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return BtsErrors.LoginFailed;

        var result = await response.Content.ReadFromJsonAsync<BtsAuthResponse>(cancellationToken);

        if (result?.Data is null)
            return BtsErrors.TokenEmpty;

        _accessToken = result.Data.AccessToken;
        _refreshToken = result.Data.RefreshToken;
        _accessTokenExpiry = DateTime.UtcNow.AddHours(23);

        logger.LogInformation("BTS: Logged in successfully");
        return Result.Success();
    }

    private async Task<Result> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (_refreshToken is null)
            return BtsErrors.RefreshFailed;

        var client = CreateHttpClient();

        var response = await client.PostAsJsonAsync("/auth/refresh",
            new { refresh_token = _refreshToken }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _refreshToken = null;
            return BtsErrors.RefreshFailed;
        }

        var result = await response.Content.ReadFromJsonAsync<BtsAuthResponse>(cancellationToken);

        if (result?.Data is null)
            return BtsErrors.TokenEmpty;

        _accessToken = result.Data.AccessToken;
        _refreshToken = result.Data.RefreshToken;
        _accessTokenExpiry = DateTime.UtcNow.AddHours(23);

        logger.LogInformation("BTS: Token refreshed successfully");
        return Result.Success();
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTime.UtcNow < _accessTokenExpiry)
            return _accessToken;

        if (_refreshToken is not null)
        {
            var refreshResult = await RefreshTokenAsync(cancellationToken);
            if (refreshResult.IsSuccess)
                return _accessToken;
        }

        var loginResult = await LoginAsync(cancellationToken);
        return loginResult.IsSuccess ? _accessToken : null;
    }

    #endregion

    #region Catalog

    public async Task<Result<List<BtsRegion>>> GetRegionsAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "bts:regions";
        if (!forceRefresh && cache.TryGetValue(cacheKey, out List<BtsRegion>? cached) && cached is not null)
            return cached;

        var response = await SendGetAsync<BtsCatalogResponse<BtsRegion>>(
            "/v1/directory/regions", cancellationToken);

        if (response?.Data?.Items is null)
            return BtsErrors.GetRegionsFailed;

        cache.Set(cacheKey, response.Data.Items, TimeSpan.FromDays(7));
        return response.Data.Items;
    }

    public async Task<Result<List<BtsCity>>> GetCitiesAsync(string regionCode, bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"bts:cities:{regionCode}";
        if (!forceRefresh && cache.TryGetValue(cacheKey, out List<BtsCity>? cached) && cached is not null)
            return cached;

        var response = await SendGetAsync<BtsCatalogResponse<BtsCity>>(
            $"/v1/directory/cities?regionCode={regionCode}", cancellationToken);

        if (response?.Data?.Items is null)
            return BtsErrors.GetCitiesFailed;

        cache.Set(cacheKey, response.Data.Items, TimeSpan.FromDays(7));
        return response.Data.Items;
    }

    public async Task<Result<List<BtsBranch>>> GetBranchesAsync(string regionCode, string cityCode,
        bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"bts:branches:{regionCode}:{cityCode}";
        if (!forceRefresh && cache.TryGetValue(cacheKey, out List<BtsBranch>? cached) && cached is not null)
            return cached;

        var response = await SendGetAsync<BtsCatalogResponse<BtsBranch>>(
            $"/v1/directory/branches?regionCode={regionCode}&cityCode={cityCode}", cancellationToken);

        if (response?.Data?.Items is null)
            return BtsErrors.GetBranchesFailed;

        cache.Set(cacheKey, response.Data.Items, TimeSpan.FromDays(1));
        return response.Data.Items;
    }

    public async Task<Result<List<BtsPackageType>>> GetPackageTypesAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "bts:packages";
        if (!forceRefresh && cache.TryGetValue(cacheKey, out List<BtsPackageType>? cached) && cached is not null)
            return cached;

        var response = await SendGetAsync<BtsCatalogResponse<BtsPackageType>>(
            "/v1/package/index", cancellationToken);

        if (response?.Data?.Items is null)
            return BtsErrors.GetPackageTypesFailed;

        cache.Set(cacheKey, response.Data.Items, TimeSpan.FromDays(30));
        return response.Data.Items;
    }

    public async Task<Result<List<BtsPostType>>> GetPostTypesAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "bts:posttypes";
        if (!forceRefresh && cache.TryGetValue(cacheKey, out List<BtsPostType>? cached) && cached is not null)
            return cached;

        var response = await SendGetAsync<BtsCatalogResponse<BtsPostType>>(
            "/v1/inside/index", cancellationToken);

        if (response?.Data?.Items is null)
            return BtsErrors.GetPostTypesFailed;

        cache.Set(cacheKey, response.Data.Items, TimeSpan.FromDays(30));
        return response.Data.Items;
    }

    #endregion

    #region Orders

    public async Task<Result<BtsOrderData>> CreateOrderAsync(BtsCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendPostAsync<BtsOrderResponse>("/v1/order/add", request, cancellationToken);

        if (response is null || !response.Status || response.Data is null)
            return BtsErrors.CreateOrderFailed;

        logger.LogInformation("BTS: Created order {BtsOrderId} for {ClientId}",
            response.Data.OrderId, request.ClientId);

        return response.Data;
    }

    public async Task<Result<BtsOrderData>> EditOrderAsync(long orderId, BtsCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendPostAsync<BtsOrderResponse>(
            $"/v1/order/edit?orderId={orderId}", request, cancellationToken);

        if (response is null || !response.Status || response.Data is null)
            return BtsErrors.EditOrderFailed;

        return response.Data;
    }

    public async Task<Result<BtsOrderData>> GetOrderDetailAsync(long orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await SendGetAsync<BtsOrderResponse>(
            $"/v1/order/detail?orderId={orderId}", cancellationToken);

        if (response is null || response.Data is null)
            return BtsErrors.GetOrderDetailFailed;

        return response.Data;
    }

    public async Task<Result> CancelOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var response = await SendGetAsync<BtsOrderResponse>(
            $"/v1/order-cancel/index?orderId={orderId}", cancellationToken);

        if (response is null)
            return BtsErrors.CancelOrderFailed;

        if (!response.Status)
            return response.Message?.Contains("already_cancelled") == true
                ? BtsErrors.OrderAlreadyCancelled
                : BtsErrors.CancelOrderFailed;

        logger.LogInformation("BTS: Cancelled order {BtsOrderId}", orderId);
        return Result.Success();
    }

    #endregion

    #region Tracking

    public async Task<Result<BtsTrackData>> TrackOrderAsync(long orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await SendGetAsync<BtsTrackResponse>(
            $"/v1/order/track?orderId={orderId}", cancellationToken);

        if (response is null || response.Data is null)
            return BtsErrors.TrackOrderFailed;

        return response.Data;
    }

    public async Task<Result<BtsStickerData>> GetStickerAsync(long orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await SendGetAsync<BtsStickerResponse>(
            $"/v1/order/sticker?orderId={orderId}", cancellationToken);

        if (response is null || response.Data is null)
            return BtsErrors.GetStickerFailed;

        return response.Data;
    }

    #endregion

    #region Calculator

    public async Task<Result<BtsCalculateData>> CalculateAsync(BtsCalculateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendPostAsync<BtsCalculateResponse>(
            "/v1/order-calculate/index", request, cancellationToken);

        if (response is null || response.Data is null)
            return BtsErrors.CalculateFailed;

        return response.Data;
    }

    #endregion

    #region HTTP Helpers

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient("BtsExpress");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("language", "uz");
        return client;
    }

    private async Task<T?> SendGetAsync<T>(string path, CancellationToken cancellationToken, bool isRetry = false)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null) return default;

        const int maxAttempts = 6;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var client = CreateHttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync(path, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetry)
                {
                    await RefreshTokenAsync(cancellationToken);
                    return await SendGetAsync<T>(path, cancellationToken, isRetry: true);
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Vaqtinchalik xatolar (5xx / 429 / 408) → qayta urinamiz; boshqasi → to'xtaymiz.
                    if (IsTransient(response.StatusCode) && attempt < maxAttempts)
                    {
                        // 429 da server bergan Retry-After ni hurmat qilamiz, aks holda eksponensial backoff.
                        var delay = response.StatusCode == HttpStatusCode.TooManyRequests
                            ? RetryAfterMs(response, attempt)
                            : RetryDelayMs(attempt);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    logger.LogWarning("BTS GET {Path} failed with {StatusCode}", path, response.StatusCode);
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            // Timeout / tarmoq uzilishi — abort qilmaymiz, qayta urinamiz (haqiqiy cancel bundan mustasno).
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException)
                                       && !cancellationToken.IsCancellationRequested)
            {
                if (attempt >= maxAttempts)
                {
                    logger.LogWarning(ex, "BTS GET {Path} failed after {Attempts} attempts", path, attempt);
                    return default;
                }

                await Task.Delay(RetryDelayMs(attempt), cancellationToken);
            }
        }

        return default;
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout            // 408
            or HttpStatusCode.TooManyRequests                 // 429
            or HttpStatusCode.InternalServerError             // 500
            or HttpStatusCode.BadGateway                      // 502
            or HttpStatusCode.ServiceUnavailable              // 503
            or HttpStatusCode.GatewayTimeout;                 // 504

    private static int RetryDelayMs(int attempt) => attempt * 500;

    // 429 uchun: server bergan Retry-After (sekund yoki sana) ni ishlatamiz; bo'lmasa
    // eksponensial backoff (1s, 2s, 4s, 8s ... 30s gacha).
    private static int RetryAfterMs(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            return (int)Math.Min(delta.TotalMilliseconds, 30_000);

        if (retryAfter?.Date is { } date)
        {
            var ms = (date - DateTimeOffset.UtcNow).TotalMilliseconds;
            if (ms > 0) return (int)Math.Min(ms, 30_000);
        }

        return Math.Min(30_000, 1000 * (int)Math.Pow(2, attempt - 1));
    }

    private async Task<T?> SendPostAsync<T>(string path, object body, CancellationToken cancellationToken,
        bool isRetry = false)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null) return default;

        var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(path, body, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetry)
        {
            await RefreshTokenAsync(cancellationToken);
            return await SendPostAsync<T>(path, body, cancellationToken, isRetry: true);
        }

        if (!response.IsSuccessStatusCode)
        {
            // BTS qaytargan xato matnini ham yozamiz — validation sabablari shu yerda bo'ladi.
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("BTS POST {Path} failed with {StatusCode}: {Body}",
                path, response.StatusCode, errorBody);
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    #endregion
}
