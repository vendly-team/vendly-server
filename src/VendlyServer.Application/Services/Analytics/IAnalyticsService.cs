using VendlyServer.Application.Services.Analytics.Contracts;

namespace VendlyServer.Application.Services.Analytics;

public interface IAnalyticsService
{
    Task<Result> TrackEventAsync(TrackEventRequest request, CancellationToken cancellationToken = default);
    Task<Result> TrackPurchaseAsync(TrackPurchaseRequest request, CancellationToken cancellationToken = default);
}
