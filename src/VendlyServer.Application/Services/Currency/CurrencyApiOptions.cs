namespace VendlyServer.Application.Services.Currency;

public class CurrencyApiOptions
{
    public const string SectionName = "CurrencyApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "USD";
    public int CacheDurationMinutes { get; set; } = 5;
    public int LockTimeoutSeconds { get; set; } = 5;
}
