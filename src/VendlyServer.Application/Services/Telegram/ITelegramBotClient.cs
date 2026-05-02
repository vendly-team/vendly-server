namespace VendlyServer.Application.Services.Telegram;

public interface ITelegramBotClient
{
    Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default);

    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);

    Task AnswerInlineQueryAsync(
        string inlineQueryId,
        IReadOnlyList<Dictionary<string, object?>> results,
        int cacheTimeSeconds,
        CancellationToken cancellationToken = default);
}
