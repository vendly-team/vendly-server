using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.Cbu;

public static class CbuCurrencyErrors
{
    public static readonly Error GetCurrencyRatesFailed = Error.Failure("CbuCurrency.GetCurrencyRates.Failed");
    public static readonly Error UsdRateNotFound = Error.NotFound("CbuCurrency.UsdRate.NotFound");
    public static readonly Error InvalidUsdRate = Error.Failure("CbuCurrency.UsdRate.Invalid");
}
