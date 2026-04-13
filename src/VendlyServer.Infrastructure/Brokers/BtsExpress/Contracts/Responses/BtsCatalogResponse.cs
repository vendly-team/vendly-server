using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

public class BtsCatalogResponse<T>
{
    [JsonPropertyName("data")]
    public BtsCatalogData<T>? Data { get; set; }
}

public class BtsCatalogData<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();
}

public class BtsRegion
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BtsCity
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BtsBranch
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("regionCode")]
    public string RegionCode { get; set; } = string.Empty;

    [JsonPropertyName("cityCode")]
    public string CityCode { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("lat_long")]
    public string LatLong { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("working_hours")]
    public Dictionary<string, string?> WorkingHours { get; set; } = new();
}

public class BtsPackageType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BtsPostType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
