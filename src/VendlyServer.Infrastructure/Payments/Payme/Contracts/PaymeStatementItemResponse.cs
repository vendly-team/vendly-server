using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeStatementItemResponse
{
    // Payme tranzaksiya id (CreateTransaction'dagi params.id).
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("account")]
    public PaymeWebhookAccountRequest Account { get; init; } = new();

    [JsonPropertyName("create_time")]
    public long CreateTime { get; init; }

    [JsonPropertyName("perform_time")]
    public long PerformTime { get; init; }

    [JsonPropertyName("cancel_time")]
    public long CancelTime { get; init; }

    [JsonPropertyName("transaction")]
    public string Transaction { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public int State { get; init; }

    [JsonPropertyName("reason")]
    public int? Reason { get; init; }
}
