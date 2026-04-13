using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;

public class BtsCalculateRequest
{
    [JsonPropertyName("senderCityCode")]
    public string SenderCityCode { get; set; } = string.Empty;

    [JsonPropertyName("receiverCityCode")]
    public string ReceiverCityCode { get; set; } = string.Empty;

    [JsonPropertyName("pickup_type")]
    public string PickupType { get; set; } = "self";

    [JsonPropertyName("dropoff_type")]
    public string DropoffType { get; set; } = "courier";

    [JsonPropertyName("is_multiple_cost")]
    public int IsMultipleCost { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("volume")]
    public BtsVolume? Volume { get; set; }
}

public class BtsVolume
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}
