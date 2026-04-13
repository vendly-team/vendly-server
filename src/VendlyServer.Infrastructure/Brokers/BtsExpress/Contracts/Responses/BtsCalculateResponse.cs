using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

public class BtsCalculateResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("data")]
    public BtsCalculateData? Data { get; set; }
}

public class BtsCalculateData
{
    [JsonPropertyName("branch_to_branch")]
    public BtsPriceOption? BranchToBranch { get; set; }

    [JsonPropertyName("branch_to_courier")]
    public BtsPriceOption? BranchToCourier { get; set; }

    [JsonPropertyName("courier_to_branch")]
    public BtsPriceOption? CourierToBranch { get; set; }

    [JsonPropertyName("courier_to_courier")]
    public BtsPriceOption? CourierToCourier { get; set; }
}

public class BtsPriceOption
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}
