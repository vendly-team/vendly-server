using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Cbu.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Cbu;

public class CbuCurrencyBroker(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<CbuCurrencyBroker> logger) : ICbuCurrencyBroker
{
    private const string ClientName = "CbuCurrency";
    private const string CacheKey = "cbu:currency:usd-rate";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<Result<CbuUsdRateResponse>> GetUsdRateAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<CbuUsdRateResponse>(CacheKey, out var cachedRate) && cachedRate is not null)
            return cachedRate;

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            var response = await client.GetAsync("uz/arkhiv-kursov-valyut/json/", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CBU currency rates request failed with status code {StatusCode}", response.StatusCode);
                return CbuCurrencyErrors.GetCurrencyRatesFailed;
            }

            var rates = await response.Content.ReadFromJsonAsync<List<CbuCurrencyRateItem>>(cancellationToken);
            if (rates is null)
                return CbuCurrencyErrors.GetCurrencyRatesFailed;

            var usd = rates.FirstOrDefault(item =>
                string.Equals(item.CurrencyCode, "USD", StringComparison.OrdinalIgnoreCase));

            if (usd is null)
                return CbuCurrencyErrors.UsdRateNotFound;

            if (!decimal.TryParse(usd.Rate, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)
                || !decimal.TryParse(usd.Diff, NumberStyles.Number, CultureInfo.InvariantCulture, out var diff)
                || string.IsNullOrWhiteSpace(usd.Date))
            {
                return CbuCurrencyErrors.InvalidUsdRate;
            }

            var result = new CbuUsdRateResponse(rate, diff, usd.Date);
            cache.Set(CacheKey, result, CacheDuration);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch USD currency rate from CBU");
            return CbuCurrencyErrors.GetCurrencyRatesFailed;
        }
    }
}
