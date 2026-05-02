using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Currency;
using VendlyServer.Application.Services.Currency.Contracts;

namespace VendlyServer.Tests.Services;

public class CurrencyConverterServiceTests
{
    private readonly FakeCurrencyApiClient _client = new();
    private readonly CurrencyConverterService _service;

    public CurrencyConverterServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CurrencyApiOptions
        {
            BaseUrl = "https://example.com/",
            ApiKey = "test-key",
            BaseCurrency = "USD",
            CacheDurationMinutes = 5,
            LockTimeoutSeconds = 5
        });

        _service = new CurrencyConverterService(
            cache,
            _client,
            options,
            NullLogger<CurrencyConverterService>.Instance);
    }

    [Fact]
    public async Task Convert_ReturnsConvertedAmount_ForCrossCurrency()
    {
        _client.Rates["EUR"] = 0.8m;
        _client.Rates["UZS"] = 12_600m;

        var result = await _service.ConvertAsync("EUR", "UZS", 10);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Data!.FromCurrency);
        Assert.Equal("UZS", result.Data.ToCurrency);
        Assert.Equal(10m, result.Data.Amount);
        Assert.Equal(157_500m, result.Data.ConvertedAmount);
        Assert.Equal(0.8m, result.Data.FromRate);
        Assert.Equal(12_600m, result.Data.ToRate);
    }

    [Fact]
    public async Task Convert_UsesCache_ForRepeatedRequests()
    {
        _client.Rates["EUR"] = 0.9m;

        var first = await _service.ConvertAsync("USD", "EUR", 100);
        var second = await _service.ConvertAsync("USD", "EUR", 200);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, _client.CallCount["EUR"]);
    }

    [Fact]
    public async Task Convert_ReturnsValidationError_WhenAmountIsNotPositive()
    {
        var result = await _service.ConvertAsync("USD", "EUR", 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(CurrencyErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public async Task Convert_ReturnsNotFound_WhenCurrencyRateDoesNotExist()
    {
        var result = await _service.ConvertAsync("USD", "AAA", 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(CurrencyErrors.CurrencyNotFound("AAA"), result.Error);
    }

    private sealed class FakeCurrencyApiClient : ICurrencyApiClient
    {
        public Dictionary<string, int> CallCount { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> Rates { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<decimal?> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            CallCount[currencyCode] = CallCount.GetValueOrDefault(currencyCode) + 1;
            return Task.FromResult<decimal?>(Rates.GetValueOrDefault(currencyCode));
        }
    }
}
