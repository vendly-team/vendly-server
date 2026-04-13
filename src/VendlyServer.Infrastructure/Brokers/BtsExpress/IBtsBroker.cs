using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress;

public interface IBtsBroker
{
    // Auth
    Task<Result> LoginAsync(CancellationToken cancellationToken = default);

    // Catalog
    Task<Result<List<BtsRegion>>> GetRegionsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Result<List<BtsCity>>> GetCitiesAsync(string regionCode, bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Result<List<BtsBranch>>> GetBranchesAsync(string regionCode, string cityCode, bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Result<List<BtsPackageType>>> GetPackageTypesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Result<List<BtsPostType>>> GetPostTypesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    // Orders
    Task<Result<BtsOrderData>> CreateOrderAsync(BtsCreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<BtsOrderData>> EditOrderAsync(long orderId, BtsCreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<BtsOrderData>> GetOrderDetailAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Result> CancelOrderAsync(long orderId, CancellationToken cancellationToken = default);

    // Tracking
    Task<Result<BtsTrackData>> TrackOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Result<BtsStickerData>> GetStickerAsync(long orderId, CancellationToken cancellationToken = default);

    // Calculator
    Task<Result<BtsCalculateData>> CalculateAsync(BtsCalculateRequest request, CancellationToken cancellationToken = default);
}
