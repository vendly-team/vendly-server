using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeWebhookParamsRequest
{
    // Payme tranzaksiya id.
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    // Payme timestamp, unix ms.
    [JsonPropertyName("time")]
    public long? Time { get; init; }

    // Summa tiyinda.
    [JsonPropertyName("amount")]
    public long? Amount { get; init; }

    [JsonPropertyName("from")]
    public long? From { get; init; }

    [JsonPropertyName("to")]
    public long? To { get; init; }

    [JsonPropertyName("reason")]
    public int? Reason { get; init; }

    [JsonPropertyName("account")]
    public PaymeWebhookAccountRequest? Account { get; init; }
}
