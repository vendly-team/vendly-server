using System.Text.Json.Serialization;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Checkout.Contracts;

/// <summary>Which payment provider to start the draft order's payment with (Hamkor / Payme / Click).</summary>
public record InitiatePaymentRequest
{
    // Global JSON enum'larni raqam sifatida o'qiydi; bu yerda nom ("Payme") qabul qilish uchun string converter.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentProvider Provider { get; init; }
}
