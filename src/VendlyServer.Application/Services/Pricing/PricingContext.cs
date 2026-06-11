using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Pricing;

// Bitta read/checkout davomida USD rate va aktiv category rule'larni
// bir marta yuklab, har bir variant uchun qayta-qayta hisoblaydi.
public sealed class PricingContext(
    decimal usdRate,
    IReadOnlyDictionary<long, CategoryPriceRule> rulesByCategory,
    PricingOptions defaults)
{
    public decimal CalculateSoumPrice(decimal usdPrice, long categoryId)
    {
        var soum = usdPrice * usdRate;

        var rule = rulesByCategory.GetValueOrDefault(categoryId);

        var markup = rule is null
            ? soum * defaults.DefaultMarkupPercent / 100m
            : rule.MarkupType == PriceMarkupType.Percent
                ? soum * rule.Value / 100m
                : rule.Value;

        var final = soum + markup;

        var step = rule?.RoundingStep ?? defaults.DefaultRoundingStep;

        return step > 0 ? Math.Ceiling(final / step) * step : final;
    }
}

public sealed record CategoryPriceRule(PriceMarkupType MarkupType, decimal Value, decimal? RoundingStep);
