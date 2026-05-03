using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.Telegram.Contracts;

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

    public async Task SendMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken = default,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        long? replyToMessageId = null)
    {
        var response = await httpClient.PostAsJsonAsync(
            "sendMessage",
            new SendMessageRequest(
                chatId,
                text,
                replyMarkup,
                replyToMessageId.HasValue
                    ? new TelegramReplyParameters(replyToMessageId.Value)
                    : null),
            cancellationToken);

        await EnsureSuccessAsync(response, "sendMessage", cancellationToken);
    }

    public async Task SendAnimationAsync(
        long chatId,
        string animationUrl,
        string caption,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "sendAnimation",
            new SendAnimationRequest(chatId, animationUrl, caption, replyMarkup),
            cancellationToken);

        await EnsureSuccessAsync(response, "sendAnimation", cancellationToken);
    }

    public async Task SendDocumentAsync(
        long chatId,
        string documentUrl,
        string caption,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "sendDocument",
            new SendDocumentRequest(chatId, documentUrl, caption, replyMarkup),
            cancellationToken);

        await EnsureSuccessAsync(response, "sendDocument", cancellationToken);
    }

    public async Task SetMessageReactionAsync(
        long chatId,
        long messageId,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "setMessageReaction",
            new SetMessageReactionRequest(
                chatId,
                messageId,
                [new TelegramReaction("emoji", emoji)]),
            cancellationToken);

        await EnsureSuccessAsync(response, "setMessageReaction", cancellationToken);
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
        [property: JsonPropertyName("reply_markup")] TelegramInlineKeyboardMarkup? ReplyMarkup,
        [property: JsonPropertyName("reply_parameters")] TelegramReplyParameters? ReplyParameters,
        [property: JsonPropertyName("parse_mode")] string ParseMode = "HTML",
        [property: JsonPropertyName("disable_web_page_preview")] bool DisableWebPagePreview = true);

    private sealed record TelegramReplyParameters(
        [property: JsonPropertyName("message_id")] long MessageId);

    private sealed record SendAnimationRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("animation")] string Animation,
        [property: JsonPropertyName("caption")] string Caption,
        [property: JsonPropertyName("reply_markup")] TelegramInlineKeyboardMarkup? ReplyMarkup,
        [property: JsonPropertyName("parse_mode")] string ParseMode = "HTML");

    private sealed record SendDocumentRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("document")] string Document,
        [property: JsonPropertyName("caption")] string Caption,
        [property: JsonPropertyName("reply_markup")] TelegramInlineKeyboardMarkup? ReplyMarkup,
        [property: JsonPropertyName("parse_mode")] string ParseMode = "HTML",
        [property: JsonPropertyName("disable_content_type_detection")] bool DisableContentTypeDetection = true);

    private sealed record SetMessageReactionRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("message_id")] long MessageId,
        [property: JsonPropertyName("reaction")] IReadOnlyList<TelegramReaction> Reaction);

    private sealed record TelegramReaction(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("emoji")] string Emoji);

    private sealed record AnswerInlineQueryRequest(
        [property: JsonPropertyName("inline_query_id")] string InlineQueryId,
        [property: JsonPropertyName("results")] IReadOnlyList<Dictionary<string, object?>> Results,
        [property: JsonPropertyName("cache_time")] int CacheTime,
        [property: JsonPropertyName("is_personal")] bool IsPersonal = false);
}
