using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeStatementResponse
{
    [JsonPropertyName("transactions")]
    public List<PaymeStatementItemResponse> Transactions { get; init; } = new();
}
