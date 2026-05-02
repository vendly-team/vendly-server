using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramBotClient(
    HttpClient httpClient,
    ILogger<TelegramBotClient> logger) : ITelegramBotClient
{
    public async Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "setWebhook",
            new SetWebhookRequest(url, secretToken),
            cancellationToken);

        await EnsureSuccessAsync(response, "setWebhook", cancellationToken);
    }

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "sendMessage",
            new SendMessageRequest(chatId, text),
            cancellationToken);

        await EnsureSuccessAsync(response, "sendMessage", cancellationToken);
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

        await EnsureSuccessAsync(response, "answerInlineQuery", cancellationToken);
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string method,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning(
            "Telegram API method {Method} failed with status {StatusCode}: {ResponseBody}",
            method,
            (int)response.StatusCode,
            responseBody);

        throw new HttpRequestException(
            $"Telegram API method {method} failed with status {(int)response.StatusCode}: {responseBody}",
            null,
            response.StatusCode);
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
