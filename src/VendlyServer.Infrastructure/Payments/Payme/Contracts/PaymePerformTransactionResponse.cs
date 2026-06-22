using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymePerformTransactionResponse
{
    [JsonPropertyName("transaction")]
    public string Transaction { get; init; } = string.Empty;

    [JsonPropertyName("perform_time")]
    public long PerformTime { get; init; }

    [JsonPropertyName("state")]
    public int State { get; init; }
}
