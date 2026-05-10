namespace VendlyServer.Application.Services.RecentlyViewed.Contracts;

public record BulkSyncRecentlyViewedRequest(IReadOnlyList<long> ProductIds);
