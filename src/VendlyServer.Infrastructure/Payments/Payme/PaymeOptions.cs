namespace VendlyServer.Infrastructure.Payments.Payme;

// "Payments:Payme" bo'limidan bind qilinadi (merchant kabinet qiymatlari).
public class PaymeOptions
{
    public const string SectionName = "Payments:Payme";

    public string MerchantId { get; set; } = string.Empty;

    // Basic auth login — Payme (Merchant API) doim "Paycom" yuboradi.
    public string Login { get; set; } = "Paycom";

    // Merchant API kaliti (sandbox'da test, prod'da jonli kalit).
    public string Password { get; set; } = string.Empty;

    // Payme checkout bazaviy URL (oxirida slash shart emas).
    public string CheckoutBase { get; set; } = "https://checkout.paycom.uz";

    // Qo'shimcha komissiya (foiz). Default 0 — aniq moslik (Payme spec; komissiya merchant hisobidan).
    public decimal CommissionPercent { get; set; } = 0m;
}
