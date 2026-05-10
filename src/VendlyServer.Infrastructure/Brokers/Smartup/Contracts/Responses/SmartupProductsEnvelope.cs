using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

public class SmartupProductsEnvelope
{
    [JsonPropertyName("count")]
    public string Count { get; set; } = "0";

    [JsonPropertyName("page_count")]
    public string PageCount { get; set; } = "0";

    [JsonPropertyName("products")]
    public List<SmartupProductItem> Products { get; set; } = [];

    public int GetPageCount() => int.TryParse(PageCount, out var p) ? p : 0;
}
