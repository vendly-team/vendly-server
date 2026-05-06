namespace VendlyServer.Application.Services.Currency;

public class CurrencyApiResponse
{
    public Dictionary<string, decimal> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
