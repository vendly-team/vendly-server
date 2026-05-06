namespace VendlyServer.Application.Services.Currencies;

public interface ICurrencyApiClient
{
    Task<decimal?> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default);
}
