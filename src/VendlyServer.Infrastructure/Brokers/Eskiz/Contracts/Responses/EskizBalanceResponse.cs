using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Eskiz.Contracts.Responses;

public class EskizBalanceResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public EskizBalanceData? Data { get; set; }
}

public class EskizBalanceData
{
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}
