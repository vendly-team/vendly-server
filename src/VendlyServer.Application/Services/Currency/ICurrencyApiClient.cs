namespace VendlyServer.Application.Services.Currency;

public interface ICurrencyApiClient
{
    Task<decimal?> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default);
}
