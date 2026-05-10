using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

public class SmartupProductItem
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("gen_name")]
    public string? GenName { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";

    [JsonPropertyName("balance_quant")]
    public string BalanceQuant { get; set; } = "0";

    [JsonPropertyName("photo_sha")]
    public List<string> PhotoSha { get; set; } = [];

    [JsonPropertyName("measure_short_name")]
    public string? MeasureShortName { get; set; }

    [JsonPropertyName("weight_netto")]
    public string? WeightNetto { get; set; }

    [JsonPropertyName("weight_brutto")]
    public string? WeightBrutto { get; set; }

    [JsonPropertyName("currency_id")]
    public string? CurrencyId { get; set; }

    [JsonPropertyName("price_label")]
    public string? PriceLabel { get; set; }
}
