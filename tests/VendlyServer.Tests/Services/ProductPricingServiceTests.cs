using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Currencies;
using VendlyServer.Application.Services.Currencies.Contracts;
using VendlyServer.Application.Services.Pricing;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class ProductPricingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeCurrencyConverter _currency = new();

    public ProductPricingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
    }

    private ProductPricingService CreateService(decimal defaultMarkupPercent = 0m, decimal defaultRoundingStep = 0m)
    {
        var opts = Options.Create(new PricingOptions
        {
            DefaultMarkupPercent = defaultMarkupPercent,
            DefaultRoundingStep = defaultRoundingStep
        });
        return new ProductPricingService(_db, _currency, opts);
    }

    // ── CreateContextAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateContext_ReturnsRateUnavailable_WhenRateFails()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Failure(CurrencyErrors.InvalidAmount);

        var result = await CreateService().CreateContextAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PricingErrors.RateUnavailable, result.Error);
    }

    [Fact]
    public async Task CreateContext_UsesRateFromConverter_WithNoRules()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Success(new(12_000m, 0m, "01.01.2026"));

        var result = await CreateService(defaultMarkupPercent: 0m).CreateContextAsync();

        Assert.True(result.IsSuccess);
        // No rule for category 1 → 0 default markup → 1 * 12000 = 12000
        Assert.Equal(12_000m, result.Data!.CalculateSoumPrice(usdPrice: 1m, categoryId: 1));
    }

    [Fact]
    public async Task CreateContext_LoadsActiveRule_ForCategory()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Success(new(10_000m, 0m, "01.01.2026"));

        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 1, CategoryId = 5, MarkupType = PriceMarkupType.Percent,
            Value = 10m, RoundingStep = null, StartDate = null, EndDate = null
        });
        await _db.SaveChangesAsync();

        var result = await CreateService().CreateContextAsync();

        Assert.True(result.IsSuccess);
        // 1 usd * 10000 = 10000 soum, +10% = 11000, no rounding
        Assert.Equal(11_000m, result.Data!.CalculateSoumPrice(usdPrice: 1m, categoryId: 5));
    }

    [Fact]
    public async Task CreateContext_IgnoresExpiredAndFutureWindows()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Success(new(10_000m, 0m, "01.01.2026"));
        var now = DateTime.UtcNow;

        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 1, CategoryId = 7, MarkupType = PriceMarkupType.Fixed, Value = 999m,
            EndDate = now.AddDays(-1)
        });
        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 2, CategoryId = 7, MarkupType = PriceMarkupType.Fixed, Value = 888m,
            StartDate = now.AddDays(1)
        });
        await _db.SaveChangesAsync();

        var result = await CreateService().CreateContextAsync();

        Assert.True(result.IsSuccess);
        // No active rule → default 0 markup → 1 * 10000 = 10000
        Assert.Equal(10_000m, result.Data!.CalculateSoumPrice(usdPrice: 1m, categoryId: 7));
    }

    [Fact]
    public async Task CreateContext_PicksLatestWindow_WhenMultipleActive()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Success(new(1m, 0m, "01.01.2026"));
        var now = DateTime.UtcNow;

        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 1, CategoryId = 3, MarkupType = PriceMarkupType.Fixed, Value = 100m,
            StartDate = now.AddDays(-10)
        });
        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 2, CategoryId = 3, MarkupType = PriceMarkupType.Fixed, Value = 200m,
            StartDate = now.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        var result = await CreateService().CreateContextAsync();

        Assert.True(result.IsSuccess);
        // Latest window (StartDate newest) → fixed markup 200 on 0*1 + 200 = 200
        Assert.Equal(200m, result.Data!.CalculateSoumPrice(usdPrice: 0m, categoryId: 3));
    }

    [Fact]
    public async Task CreateContext_ExcludesSoftDeletedRules()
    {
        _currency.RateResult = Result<CurrencyRateResponse>.Success(new(1m, 0m, "01.01.2026"));

        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = 1, CategoryId = 9, MarkupType = PriceMarkupType.Fixed, Value = 500m,
            IsDeleted = true
        });
        await _db.SaveChangesAsync();

        var result = await CreateService(defaultMarkupPercent: 0m).CreateContextAsync();

        Assert.True(result.IsSuccess);
        // Deleted rule ignored → default 0 markup → 100 * 1 = 100
        Assert.Equal(100m, result.Data!.CalculateSoumPrice(usdPrice: 100m, categoryId: 9));
    }

    // ── PricingContext.CalculateSoumPrice (pure calc) ──────────────────────────

    [Fact]
    public void Calculate_AppliesDefaultPercentMarkup_WhenNoRule()
    {
        var ctx = new PricingContext(10_000m, new Dictionary<long, CategoryPriceRule>(),
            new PricingOptions { DefaultMarkupPercent = 20m, DefaultRoundingStep = 0m });

        // 2 usd * 10000 = 20000, +20% = 24000
        Assert.Equal(24_000m, ctx.CalculateSoumPrice(usdPrice: 2m, categoryId: 1));
    }

    [Fact]
    public void Calculate_AppliesPercentRule()
    {
        var rules = new Dictionary<long, CategoryPriceRule>
        {
            [1] = new(PriceMarkupType.Percent, 50m, null)
        };
        var ctx = new PricingContext(10_000m, rules,
            new PricingOptions { DefaultMarkupPercent = 0m, DefaultRoundingStep = 0m });

        // 1 * 10000 = 10000, +50% = 15000
        Assert.Equal(15_000m, ctx.CalculateSoumPrice(usdPrice: 1m, categoryId: 1));
    }

    [Fact]
    public void Calculate_AppliesFixedRule()
    {
        var rules = new Dictionary<long, CategoryPriceRule>
        {
            [1] = new(PriceMarkupType.Fixed, 5_000m, null)
        };
        var ctx = new PricingContext(10_000m, rules,
            new PricingOptions { DefaultMarkupPercent = 0m, DefaultRoundingStep = 0m });

        // 1 * 10000 = 10000, + fixed 5000 = 15000
        Assert.Equal(15_000m, ctx.CalculateSoumPrice(usdPrice: 1m, categoryId: 1));
    }

    [Fact]
    public void Calculate_AppliesRuleRoundingStep_CeilingUp()
    {
        var rules = new Dictionary<long, CategoryPriceRule>
        {
            [1] = new(PriceMarkupType.Fixed, 0m, 1_000m)
        };
        var ctx = new PricingContext(1m, rules,
            new PricingOptions { DefaultMarkupPercent = 0m, DefaultRoundingStep = 0m });

        // 10500 * 1 = 10500, +0 = 10500, ceil to 1000 step → 11000
        Assert.Equal(11_000m, ctx.CalculateSoumPrice(usdPrice: 10_500m, categoryId: 1));
    }

    [Fact]
    public void Calculate_UsesDefaultRoundingStep_WhenRuleStepNull()
    {
        var rules = new Dictionary<long, CategoryPriceRule>
        {
            [1] = new(PriceMarkupType.Fixed, 0m, null)
        };
        var ctx = new PricingContext(1m, rules,
            new PricingOptions { DefaultMarkupPercent = 0m, DefaultRoundingStep = 500m });

        // 1200 → ceil to 500 step → 1500
        Assert.Equal(1_500m, ctx.CalculateSoumPrice(usdPrice: 1_200m, categoryId: 1));
    }

    [Fact]
    public void Calculate_NoRounding_WhenStepZero()
    {
        var ctx = new PricingContext(1m, new Dictionary<long, CategoryPriceRule>(),
            new PricingOptions { DefaultMarkupPercent = 0m, DefaultRoundingStep = 0m });

        Assert.Equal(1_234m, ctx.CalculateSoumPrice(usdPrice: 1_234m, categoryId: 1));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private sealed class FakeCurrencyConverter : ICurrencyConverterService
    {
        public Result<CurrencyRateResponse> RateResult { get; set; } =
            Result<CurrencyRateResponse>.Success(new(1m, 0m, "01.01.2026"));

        public Task<Result<CurrencyRateResponse>> GetUsdRateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(RateResult);

        public Task<Result<CurrencyConversionResponse>> ConvertAsync(
            string fromCurrency, string toCurrency, decimal amount, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
