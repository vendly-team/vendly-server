using VendlyServer.Application.Services.Analytics;
using VendlyServer.Application.Services.Analytics.Contracts;

namespace VendlyServer.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly AnalyticsService _service = new();

    [Fact]
    public async Task TrackEvent_ReturnsSuccess()
    {
        var result = await _service.TrackEventAsync(new TrackEventRequest("client-1", "view_item", []));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TrackPurchase_ReturnsSuccess()
    {
        var result = await _service.TrackPurchaseAsync(new TrackPurchaseRequest("client-1", "txn-1", 250m, 0m, "UZS", []));

        Assert.True(result.IsSuccess);
    }
}
