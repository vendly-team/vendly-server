using System.Text.Json.Serialization;

namespace VendlyServer.Application.Services.Telegram.Contracts;

public sealed class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    [JsonPropertyName("inline_query")]
    public TelegramInlineQuery? InlineQuery { get; init; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }
}

public sealed class TelegramInlineQuery
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;
}

public sealed class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; init; }
}

public sealed class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; init; }
}
