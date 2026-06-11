using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Pricing;

public interface IProductPricingService
{
    Task<Result<PricingContext>> CreateContextAsync(CancellationToken cancellationToken = default);
}
