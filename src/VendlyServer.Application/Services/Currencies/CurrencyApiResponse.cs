namespace VendlyServer.Application.Services.Currencies;

public class CurrencyApiResponse
{
    public Dictionary<string, decimal> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
