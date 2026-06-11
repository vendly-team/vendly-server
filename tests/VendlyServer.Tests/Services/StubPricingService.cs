using VendlyServer.Application.Services.Pricing;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Services;

// Identity pricing context — rate=1, no category rules, 0 markup/rounding.
// CalculateSoumPrice narxni o'zgartirmaydi, shu sabab mavjud narx assertionlari saqlanadi.
internal sealed class StubPricingService : IProductPricingService
{
    public Task<Result<PricingContext>> CreateContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<Result<PricingContext>>(
            new PricingContext(1m, new Dictionary<long, CategoryPriceRule>(), new PricingOptions()));
}
