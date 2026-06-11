using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Currencies;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Pricing;

public class ProductPricingService(
    AppDbContext dbContext,
    ICurrencyConverterService currencyConverter,
    IOptions<PricingOptions> options) : IProductPricingService
{
    private readonly PricingOptions _options = options.Value;

    public async Task<Result<PricingContext>> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        var rateResult = await currencyConverter.GetUsdRateAsync(cancellationToken);
        if (!rateResult.IsSuccess)
            return PricingErrors.RateUnavailable;

        var now = DateTime.UtcNow;

        var activePrices = await dbContext.CategoryPrices
            .AsNoTracking()
            .Where(cp => !cp.IsDeleted)
            .Where(cp => (cp.StartDate == null || cp.StartDate <= now) &&
                         (cp.EndDate == null || cp.EndDate >= now))
            .ToListAsync(cancellationToken);

        // Bir category uchun bir nechta aktiv window bo'lsa — eng so'nggisini olamiz
        var rulesByCategory = activePrices
            .GroupBy(cp => cp.CategoryId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g
                        .OrderByDescending(cp => cp.StartDate ?? DateTime.MinValue)
                        .ThenByDescending(cp => cp.CreatedAt)
                        .First();

                    return new CategoryPriceRule(latest.MarkupType, latest.Value, latest.RoundingStep);
                });

        return new PricingContext(rateResult.Data!.Rate, rulesByCategory, _options);
    }
}
