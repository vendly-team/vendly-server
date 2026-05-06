using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Currency.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Currency;

public class CurrencyConverterService(
    IMemoryCache cache,
    ICurrencyApiClient currencyApiClient,
    IOptions<CurrencyApiOptions> options,
    ILogger<CurrencyConverterService> logger) : ICurrencyConverterService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private readonly CurrencyApiOptions _options = options.Value;

    public async Task<Result<CurrencyConversionResponse>> ConvertAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return CurrencyErrors.InvalidAmount;

        if (!IsValidCurrencyCode(fromCurrency) || !IsValidCurrencyCode(toCurrency))
            return CurrencyErrors.InvalidCurrencyCode;

        var normalizedFrom = fromCurrency.ToUpperInvariant();
        var normalizedTo = toCurrency.ToUpperInvariant();

        if (normalizedFrom == normalizedTo)
        {
            return new CurrencyConversionResponse(
                normalizedFrom,
                normalizedTo,
                amount,
                1m,
                1m,
                amount);
        }

        var fromRateResult = await GetRateAsync(normalizedFrom, cancellationToken);
        if (!fromRateResult.IsSuccess)
            return fromRateResult.Error;

        var toRateResult = await GetRateAsync(normalizedTo, cancellationToken);
        if (!toRateResult.IsSuccess)
            return toRateResult.Error;

        var fromRate = fromRateResult.Data;
        var toRate = toRateResult.Data;
        var convertedAmount = amount / fromRate * toRate;

        return new CurrencyConversionResponse(
            normalizedFrom,
            normalizedTo,
            amount,
            fromRate,
            toRate,
            convertedAmount);
    }

    private async Task<Result<decimal>> GetRateAsync(
        string currencyCode,
        CancellationToken cancellationToken)
    {
        if (currencyCode.Equals(_options.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var cacheKey = GetCacheKey(currencyCode);
        if (cache.TryGetValue<decimal>(cacheKey, out var cachedRate))
            return cachedRate;

        var semaphore = Locks.GetOrAdd(currencyCode, _ => new SemaphoreSlim(1, 1));

        var acquired = await semaphore.WaitAsync(
            TimeSpan.FromSeconds(_options.LockTimeoutSeconds),
            cancellationToken);

        if (!acquired)
        {
            logger.LogWarning("Timed out waiting for currency lock for {CurrencyCode}", currencyCode);
            return CurrencyErrors.ProviderUnavailable;
        }

        try
        {
            if (cache.TryGetValue<decimal>(cacheKey, out cachedRate))
                return cachedRate;

            var rate = await currencyApiClient.GetExchangeRateAsync(currencyCode, cancellationToken);
            if (rate is null)
                return CurrencyErrors.CurrencyNotFound(currencyCode);

            cache.Set(cacheKey, rate.Value, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
            return rate.Value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static bool IsValidCurrencyCode(string currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode)
               && currencyCode.Length == 3
               && currencyCode.All(char.IsLetter);
    }

    private static string GetCacheKey(string currencyCode) => $"currency-rate:{currencyCode}";
}
