using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

// "account" obyekti — checkout URL'dagi ac.* paramlarga mos. Bizda ac.order_id = Order.Id.
public record PaymeWebhookAccountRequest
{
    [JsonPropertyName("order_id")]
    public string? OrderId { get; init; }
}
