using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

public class SmartupCategoryItem
{
    [JsonPropertyName("product_type_id")]
    public string ProductTypeId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("style")]
    public SmartupCategoryStyle? Style { get; set; }
}

public class SmartupCategoryStyle
{
    [JsonPropertyName("l")]
    public SmartupCategoryStyleDetail? L { get; set; }
}

public class SmartupCategoryStyleDetail
{
    [JsonPropertyName("photo_sha")]
    public string? PhotoSha { get; set; }
}
