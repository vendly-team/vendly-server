using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeCheckTransactionResponse
{
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
