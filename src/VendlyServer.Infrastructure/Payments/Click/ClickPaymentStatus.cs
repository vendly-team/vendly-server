namespace VendlyServer.Infrastructure.Payments.Click;

// Click Merchant API v2 payment_status qiymatlari (rasmiy hujjat).
// Manba: https://docs.click.uz/en/merchant-api-request/  (GET /payment/status)
public enum ClickPaymentStatus
{
    // Manfiy — bekor qilingan/xato.
    Cancelled = -9,
    Failed = -5,
    Refunded = -4,

    // 0 — kutilmoqda / aniqlanmagan.
    Pending = 0,

    // 2 — muvaffaqiyatli amalga oshirilgan to'lov.
    Success = 2,
}
