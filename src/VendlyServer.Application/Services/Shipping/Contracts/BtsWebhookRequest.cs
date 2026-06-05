using System.Text.Json.Serialization;

namespace VendlyServer.Application.Services.Shipping.Contracts;

/// <summary>
/// Payload BTS Express POSTs to our webhook when a shipment's status changes.
/// NOTE: confirm the exact field shape against the BTS webhook docs.
/// </summary>
public record BtsWebhookRequest
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; init; }
}
