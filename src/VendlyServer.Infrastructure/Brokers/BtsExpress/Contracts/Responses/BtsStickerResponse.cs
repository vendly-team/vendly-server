using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

public class BtsStickerResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("data")]
    public BtsStickerData? Data { get; set; }
}

public class BtsStickerData
{
    [JsonPropertyName("labelEncode")]
    public string? LabelEncode { get; set; }

    [JsonPropertyName("labelSticker")]
    public string? LabelSticker { get; set; }
}
