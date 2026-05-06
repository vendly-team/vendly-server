using VendlyServer.Application.Services.Currency.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Currency;

public interface ICurrencyConverterService
{
    Task<Result<CurrencyConversionResponse>> ConvertAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default);
}
