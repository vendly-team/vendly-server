namespace VendlyServer.Application.Services.Currency.Contracts;

public record CurrencyConversionResponse(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal FromRate,
    decimal ToRate,
    decimal ConvertedAmount);
