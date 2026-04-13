using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;

public class BtsCreateOrderRequest
{
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("pickup_type")]
    public string PickupType { get; set; } = "self";

    [JsonPropertyName("dropoff_type")]
    public string DropoffType { get; set; } = "courier";

    [JsonPropertyName("is_sender_location")]
    public bool IsSenderLocation { get; set; }

    [JsonPropertyName("is_receiver_location")]
    public bool IsReceiverLocation { get; set; }

    [JsonPropertyName("sender")]
    public BtsParty Sender { get; set; } = new();

    [JsonPropertyName("receiver")]
    public BtsParty Receiver { get; set; } = new();

    [JsonPropertyName("bringBackMoney")]
    public int BringBackMoney { get; set; }

    [JsonPropertyName("back_money")]
    public decimal BackMoney { get; set; }

    [JsonPropertyName("takePhoto")]
    public int TakePhoto { get; set; } = 1;

    [JsonPropertyName("bringBackWaybill")]
    public int BringBackWaybill { get; set; }

    [JsonPropertyName("cargo")]
    public BtsCargo Cargo { get; set; } = new();

    [JsonPropertyName("ready_to_take")]
    public bool ReadyToTake { get; set; } = true;
}

public class BtsParty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("phone1")]
    public string? Phone1 { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city_code")]
    public string? CityCode { get; set; }

    [JsonPropertyName("branch_code")]
    public string? BranchCode { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }
}

public class BtsCargo
{
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("volume")]
    public double Volume { get; set; }

    [JsonPropertyName("piece")]
    public int Piece { get; set; } = 1;

    [JsonPropertyName("packageId")]
    public int PackageId { get; set; } = 7;

    [JsonPropertyName("postTypeId")]
    public int PostTypeId { get; set; } = 7;
}
