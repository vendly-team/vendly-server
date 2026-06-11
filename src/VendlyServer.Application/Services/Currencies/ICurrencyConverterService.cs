using VendlyServer.Application.Services.Currencies.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Currencies;

public interface ICurrencyConverterService
{
    Task<Result<CurrencyConversionResponse>> ConvertAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<Result<CurrencyRateResponse>> GetUsdRateAsync(CancellationToken cancellationToken = default);
}
