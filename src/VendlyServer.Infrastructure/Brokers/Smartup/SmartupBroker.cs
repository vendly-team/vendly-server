using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public class SmartupBroker(
    IHttpClientFactory httpClientFactory,
    IOptions<SmartupOptions> options,
    ILogger<SmartupBroker> logger) : ISmartupBroker
{
    private readonly SmartupOptions _options = options.Value;

    public async Task<SmartupCallResult<List<SmartupCategoryItem>>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            uri = "product_types",
            param = new { person_id = _options.PersonId, filial_id = _options.FilialId },
            lang_code = "uz"
        };
        var url = BuildUrl();
        var requestBody = JsonSerializer.SerializeToDocument(body);

        var (data, responseBody, httpSuccess, durationMs, startedAt, finishedAt) =
            await PostAsync<List<SmartupCategoryItem>>("api/data", body, cancellationToken);

        if (data is null)
        {
            logger.LogWarning("Smartup: GetCategories returned null");
            return new SmartupCallResult<List<SmartupCategoryItem>>(
                SmartupErrors.GetCategoriesFailed, url, requestBody, responseBody, durationMs, httpSuccess, startedAt, finishedAt);
        }

        return new SmartupCallResult<List<SmartupCategoryItem>>(
            data, url, requestBody, responseBody, durationMs, httpSuccess, startedAt, finishedAt);
    }

    public async Task<SmartupCallResult<SmartupProductsEnvelope>> GetProductsAsync(
        string productTypeId, int pageNo, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            uri = "products",
            param = new
            {
                person_id = _options.PersonId,
                filial_id = _options.FilialId,
                product_type_id = productTypeId,
                page_no = pageNo
            },
            lang_code = "uz"
        };
        var url = BuildUrl();
        var requestBody = JsonSerializer.SerializeToDocument(body);

        var (data, responseBody, httpSuccess, durationMs, startedAt, finishedAt) =
            await PostAsync<SmartupProductsEnvelope>("api/data", body, cancellationToken);

        if (data is null)
        {
            logger.LogWarning("Smartup: GetProducts for type {TypeId} page {Page} returned null",
                productTypeId, pageNo);
            return new SmartupCallResult<SmartupProductsEnvelope>(
                SmartupErrors.GetProductsFailed, url, requestBody, responseBody, durationMs, httpSuccess, startedAt, finishedAt);
        }

        return new SmartupCallResult<SmartupProductsEnvelope>(
            data, url, requestBody, responseBody, durationMs, httpSuccess, startedAt, finishedAt);
    }

    private async Task<(T? data, JsonDocument? responseBody, bool httpSuccess, int durationMs, DateTime startedAt, DateTime finishedAt)>
        PostAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var client = CreateHttpClient();
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.PostAsJsonAsync(path, body, cancellationToken);
            sw.Stop();
            var finishedAt = DateTime.UtcNow;

            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonDocument? responseBody = null;
            try { responseBody = JsonDocument.Parse(rawJson); } catch { /* ignore parse errors */ }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Smartup POST {Path} failed: {Status}", path, response.StatusCode);
                return (default, responseBody, false, (int)sw.ElapsedMilliseconds, startedAt, finishedAt);
            }

            var data = JsonSerializer.Deserialize<T>(rawJson);
            return (data, responseBody, true, (int)sw.ElapsedMilliseconds, startedAt, finishedAt);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var finishedAt = DateTime.UtcNow;
            logger.LogError(ex, "Smartup POST {Path} threw exception", path);
            return (default, null, false, (int)sw.ElapsedMilliseconds, startedAt, finishedAt);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient("Smartup");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        return client;
    }

    private string BuildUrl() => $"{_options.BaseUrl.TrimEnd('/')}/api/data";

    public async Task<Result<SmartupImageData>> DownloadImageAsync(
        string sha, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.ImageBaseUrl.TrimEnd('/')}/b/biruni/m:load_image?sha={sha}";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Token);

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Smartup: DownloadImage sha={Sha} failed: {Status}", sha, response.StatusCode);
                return SmartupErrors.DownloadImageFailed;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            return new SmartupImageData(buffer, contentType, buffer.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Smartup: DownloadImage sha={Sha} threw exception", sha);
            return SmartupErrors.DownloadImageFailed;
        }
    }
}
