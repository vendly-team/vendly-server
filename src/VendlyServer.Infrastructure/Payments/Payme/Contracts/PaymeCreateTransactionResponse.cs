using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeCreateTransactionResponse
{
    [JsonPropertyName("create_time")]
    public long CreateTime { get; init; }

    // Bizning ichki tranzaksiya id (PaymentTransaction.Id) string sifatida.
    [JsonPropertyName("transaction")]
    public string Transaction { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public int State { get; init; }
}
