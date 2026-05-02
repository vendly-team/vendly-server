namespace VendlyServer.Application.Services.Telegram;

public interface ITelegramImageUrlValidator
{
    Task<bool> IsValidThumbnailAsync(string imageUrl, CancellationToken cancellationToken = default);
}
