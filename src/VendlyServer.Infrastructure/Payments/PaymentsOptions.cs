namespace VendlyServer.Infrastructure.Payments;

// "Payments" bo'limidan bind qilinadi (env-ga xos appsettings).
public class PaymentsOptions
{
    public const string SectionName = "Payments";

    // To'lovdan keyin foydalanuvchi brauzeri qaytadigan manzil (to'lov status sahifasi).
    public string ReturnUrl { get; set; } = string.Empty;
}
