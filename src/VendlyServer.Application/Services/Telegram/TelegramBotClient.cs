using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramBotClient(HttpClient httpClient) : ITelegramBotClient
{
    public async Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "setWebhook",
            new SetWebhookRequest(url, secretToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "sendMessage",
            new SendMessageRequest(chatId, text),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task AnswerInlineQueryAsync(
        string inlineQueryId,
        IReadOnlyList<Dictionary<string, object?>> results,
        int cacheTimeSeconds,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "answerInlineQuery",
            new AnswerInlineQueryRequest(inlineQueryId, results, cacheTimeSeconds),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record SetWebhookRequest(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("secret_token")] string SecretToken);

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("parse_mode")] string ParseMode = "HTML",
        [property: JsonPropertyName("disable_web_page_preview")] bool DisableWebPagePreview = true);

    private sealed record AnswerInlineQueryRequest(
        [property: JsonPropertyName("inline_query_id")] string InlineQueryId,
        [property: JsonPropertyName("results")] IReadOnlyList<Dictionary<string, object?>> Results,
        [property: JsonPropertyName("cache_time")] int CacheTime,
        [property: JsonPropertyName("is_personal")] bool IsPersonal = false);
}
