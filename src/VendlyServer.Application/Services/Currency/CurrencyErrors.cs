using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Currency;

public static class CurrencyErrors
{
    public static readonly Error InvalidAmount = Error.Validation(
        "Currency.InvalidAmount",
        "Amount must be greater than zero.");

    public static readonly Error InvalidCurrencyCode = Error.Validation(
        "Currency.InvalidCurrencyCode",
        "Currency codes must be 3-letter alphabetic values.");

    public static readonly Error ProviderUnavailable = Error.Failure("Currency.ProviderUnavailable");

    public static Error CurrencyNotFound(string currencyCode) => Error.NotFound(
        "Currency.NotFound");
}
