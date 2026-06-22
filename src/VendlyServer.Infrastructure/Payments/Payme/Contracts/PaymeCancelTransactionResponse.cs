using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeCancelTransactionResponse
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; init; } = string.Empty;

    [JsonPropertyName("cancel_time")]
    public long CancelTime { get; init; }

    [JsonPropertyName("state")]
    public int State { get; init; }
}
