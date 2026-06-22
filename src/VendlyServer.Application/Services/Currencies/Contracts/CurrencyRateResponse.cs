namespace VendlyServer.Application.Services.Currencies.Contracts;

public record CurrencyRateResponse(
    decimal Rate,
    decimal Diff,
    string Date);
