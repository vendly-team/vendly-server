using VendlyServer.Application.Services.Analytics.Contracts;

namespace VendlyServer.Application.Services.Analytics;

// TODO: ACTIVATE — Measurement Protocol v2
// When ready, inject IHttpClientFactory and IOptions<AnalyticsOptions>.
// POST to https://www.google-analytics.com/mp/collect?measurement_id={id}&api_secret={secret}
// with payload: { client_id, events: [{ name, params }] }
public sealed class AnalyticsService : IAnalyticsService
{
    public Task<Result> TrackEventAsync(TrackEventRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> TrackPurchaseAsync(TrackPurchaseRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
