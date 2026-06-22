using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Cbu.Contracts.Responses;

public class CbuCurrencyRateItem
{
    [JsonPropertyName("Code")]
    public string? Code { get; set; }

    [JsonPropertyName("Ccy")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("Rate")]
    public string? Rate { get; set; }

    [JsonPropertyName("Diff")]
    public string? Diff { get; set; }

    [JsonPropertyName("Date")]
    public string? Date { get; set; }
}
