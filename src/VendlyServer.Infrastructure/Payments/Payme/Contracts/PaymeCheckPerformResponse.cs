using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeCheckPerformResponse
{
    [JsonPropertyName("allow")]
    public bool Allow { get; init; }
}
