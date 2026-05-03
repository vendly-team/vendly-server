using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Application.Services.Telegram;

public interface ITelegramBotClient
{
    Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default);

    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);

    Task SendAnimationAsync(
        long chatId,
        string animationUrl,
        string caption,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    Task SendDocumentAsync(
        long chatId,
        string documentUrl,
        string caption,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    Task AnswerInlineQueryAsync(
        string inlineQueryId,
        IReadOnlyList<Dictionary<string, object?>> results,
        int cacheTimeSeconds,
        CancellationToken cancellationToken = default);
}
