namespace VendlyServer.Application.Services.Currencies.Contracts;

public record CurrencyConversionResponse(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal FromRate,
    decimal ToRate,
    decimal ConvertedAmount);
