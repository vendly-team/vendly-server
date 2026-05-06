using VendlyServer.Application.Services.RecentlyViewed.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.RecentlyViewed;

public interface IRecentlyViewedService
{
    Task<Result<List<RecentlyViewedResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
    Task<Result> TrackAsync(long userId, TrackProductViewRequest request, CancellationToken cancellationToken = default);
    Task<Result> BulkSyncAsync(long userId, BulkSyncRecentlyViewedRequest request, CancellationToken cancellationToken = default);
    Task<Result> ClearAsync(long userId, CancellationToken cancellationToken = default);
}
