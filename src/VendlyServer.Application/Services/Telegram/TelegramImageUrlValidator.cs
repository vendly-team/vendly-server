using Microsoft.Extensions.Logging;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramImageUrlValidator(
    HttpClient httpClient,
    ILogger<TelegramImageUrlValidator> logger) : ITelegramImageUrlValidator
{
    private const long MaxThumbnailSizeBytes = 200 * 1024;

    public async Task<bool> IsValidThumbnailAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, imageUrl);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType) ||
                !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return false;

            var contentLength = response.Content.Headers.ContentLength;
            return !contentLength.HasValue || contentLength.Value <= MaxThumbnailSizeBytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to validate Telegram thumbnail image URL: {ImageUrl}", imageUrl);
            return false;
        }
    }
}
