using System.Text.Json;
using System.Text.Json.Serialization;

namespace VendlyServer.Application.Services.Telegram.Contracts;

public sealed class TelegramInlineKeyboardMarkup
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [JsonPropertyName("inline_keyboard")]
    public IReadOnlyList<IReadOnlyList<TelegramInlineKeyboardButton>> InlineKeyboard { get; init; } = [];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }
}

public sealed class TelegramInlineKeyboardButton
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}

public sealed class TelegramInputTextMessageContent
{
    [JsonPropertyName("message_text")]
    public string MessageText { get; init; } = string.Empty;

    [JsonPropertyName("parse_mode")]
    public string ParseMode { get; init; } = "HTML";

    [JsonPropertyName("disable_web_page_preview")]
    public bool DisableWebPagePreview { get; init; } = true;
}
