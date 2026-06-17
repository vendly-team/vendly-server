using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

// JSON-RPC 2.0 javob konverti: result yoki error dan biri o'rnatiladi.
public record PaymeRpcResultResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public PaymeErrorResponse? Error { get; init; }
}
