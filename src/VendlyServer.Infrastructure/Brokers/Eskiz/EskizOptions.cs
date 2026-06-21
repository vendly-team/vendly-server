namespace VendlyServer.Infrastructure.Brokers.Eskiz;

public class EskizOptions
{
    public const string SectionName = "Eskiz";

    // Manba root (path'lar "/api/..." bilan beriladi). Masalan: https://notify.eskiz.uz
    public string BaseUrl { get; set; } = "https://notify.eskiz.uz";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Tasdiqlangan nickname. Test rejimida 4546 ishlatiladi.
    public string From { get; set; } = "4546";

    // Eskiz status callback'ni yuboradigan URL (bo'sh bo'lsa — yuborilmaydi).
    public string CallbackUrl { get; set; } = string.Empty;
}
