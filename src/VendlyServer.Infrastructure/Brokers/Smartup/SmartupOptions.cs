namespace VendlyServer.Infrastructure.Brokers.Smartup;

public class SmartupOptions
{
    public const string SectionName = "Smartup";

    public string BaseUrl { get; set; } = string.Empty;
    public string ImageBaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;
    public string FilialId { get; set; } = string.Empty;
}
