using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

// Payme merchant API JSON-RPC 2.0 so'rov konverti.
public record PaymeWebhookRequest
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public PaymeWebhookParamsRequest Params { get; init; } = new();
}
