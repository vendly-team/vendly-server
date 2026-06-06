namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public class HamkorOptions
{
    public const string SectionName = "Hamkor";

    public string BaseUrl { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    // Our own public base URL — the bank sends the payment callback (webhook) here.
    public string CallbackBaseUrl { get; set; } = string.Empty;
}
