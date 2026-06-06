namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public class HamkorOptions
{
    public const string SectionName = "Hamkor";

    public string BaseUrl { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    // Our own public base URL — the bank sends the payment callback (webhook) here.
    public string CallbackBaseUrl { get; set; } = string.Empty;

    // Fiscal data — required by Hamkorbank's hosted payment page; without it the invoice
    // is created but lands in "Ошибка" state and the page shows "wrong invoice" (code 1014).
    public string Tin { get; set; } = string.Empty;

    public string Spic { get; set; } = string.Empty;

    public string PackageCode { get; set; } = string.Empty;

    public int VatPercent { get; set; } = 12;

    public double LocationLat { get; set; } = 41.31;

    public double LocationLong { get; set; } = 69.24;
}
