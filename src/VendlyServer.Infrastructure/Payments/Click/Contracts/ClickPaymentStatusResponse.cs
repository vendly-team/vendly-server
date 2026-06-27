using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Click.Contracts;

// Click Merchant API v2 — GET /payment/status/{service_id}/{payment_id} javobi.
public record ClickPaymentStatusResponse
{
    [JsonPropertyName("payment_id")]
    public long PaymentId { get; init; }

    [JsonPropertyName("payment_status")]
    public int PaymentStatus { get; init; }

    [JsonPropertyName("payment_status_note")]
    public string? PaymentStatusNote { get; init; }

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; init; }

    [JsonPropertyName("error_note")]
    public string? ErrorNote { get; init; }
}
