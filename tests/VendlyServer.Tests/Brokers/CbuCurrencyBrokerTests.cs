using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using VendlyServer.Infrastructure.Brokers.Cbu;

namespace VendlyServer.Tests.Brokers;

public class CbuCurrencyBrokerTests
{
    [Fact]
    public async Task GetUsdRateAsync_ReturnsUsdRateDiffAndDate()
    {
        var broker = CreateBroker("""
        [
          { "Code": "978", "Ccy": "EUR", "Rate": "14880.21", "Diff": "12.10", "Date": "06.06.2026" },
          { "Code": "840", "Ccy": "USD", "Rate": "12600.00", "Diff": "-5.23", "Date": "06.06.2026" }
        ]
        """);

        var result = await broker.GetUsdRateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(12600.00m, result.Data!.Rate);
        Assert.Equal(-5.23m, result.Data.Diff);
        Assert.Equal("06.06.2026", result.Data.Date);
    }

    [Fact]
    public async Task GetUsdRateAsync_UsesOneHourCache_ForRepeatedRequests()
    {
        var handler = new StubHttpMessageHandler("""
        [
          { "Code": "840", "Ccy": "USD", "Rate": "12600.00", "Diff": "-5.23", "Date": "06.06.2026" }
        ]
        """);
        var broker = CreateBroker(handler);

        var first = await broker.GetUsdRateAsync();
        var second = await broker.GetUsdRateAsync();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, handler.CallCount);
    }

    private static CbuCurrencyBroker CreateBroker(string json)
    {
        return CreateBroker(new StubHttpMessageHandler(json));
    }

    private static CbuCurrencyBroker CreateBroker(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://cbu.uz/")
        };
        var factory = new StubHttpClientFactory(httpClient);
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new CbuCurrencyBroker(
            factory,
            cache,
            NullLogger<CbuCurrencyBroker>.Instance);
    }

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class StubHttpMessageHandler(string json) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            return Task.FromResult(response);
        }
    }
}
