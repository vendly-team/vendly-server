using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Pricing;

public static class PricingErrors
{
    public static readonly Error RateUnavailable = Error.Failure("Pricing.RateUnavailable");
}
