using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

public class BtsOrderResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public BtsOrderData? Data { get; set; }
}

public class BtsOrderData
{
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("senderDate")]
    public string SenderDate { get; set; } = string.Empty;

    [JsonPropertyName("receiverDate")]
    public string ReceiverDate { get; set; } = string.Empty;

    [JsonPropertyName("tracking")]
    public string Tracking { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public BtsStatusInfo? StatusInfo { get; set; }
}

public class BtsStatusInfo
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("info")]
    public string? Info { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
