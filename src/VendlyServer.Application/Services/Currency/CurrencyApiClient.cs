using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace VendlyServer.Application.Services.Currency;

public class CurrencyApiClient(HttpClient httpClient, ILogger<CurrencyApiClient> logger) : ICurrencyApiClient
{
    public async Task<decimal?> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"latest?currencies={Uri.EscapeDataString(currencyCode.ToUpperInvariant())}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Currency API request failed with status code {StatusCode}",
                    response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<CurrencyApiResponse>(
                cancellationToken: cancellationToken);

            if (payload?.Data is null)
                return null;

            return payload.Data.TryGetValue(currencyCode.ToUpperInvariant(), out var rate)
                ? rate
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch exchange rate for {CurrencyCode}", currencyCode);
            return null;
        }
    }
}
