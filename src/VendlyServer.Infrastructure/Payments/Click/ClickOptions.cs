namespace VendlyServer.Infrastructure.Payments.Click;

// "Payments:Click" bo'limidan bind qilinadi (merchant kabinet qiymatlari).
public class ClickOptions
{
    public const string SectionName = "Payments:Click";

    public string MerchantId { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    // Click kabinetidagi merchant_user_id.
    public string MerchantUserId { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    // Click checkout (pay) bazaviy URL.
    public string CheckoutBase { get; set; } = "https://my.click.uz/services/pay";

    // Click mijozdan oladigan xizmat haqi (foiz). Webhook summasi baza + shu foizgacha bo'lishi mumkin.
    public decimal CommissionPercent { get; set; } = 1.0m;
}
