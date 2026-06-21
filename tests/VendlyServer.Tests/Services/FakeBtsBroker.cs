using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

namespace VendlyServer.Tests.Services;

/// <summary>Hand-written fake for the BTS broker used by shipping service tests.</summary>
internal sealed class FakeBtsBroker : IBtsBroker
{
    // ── Calculate ──
    public Result<BtsCalculateData> CalculateResult { get; set; } =
        Result<BtsCalculateData>.Failure(Error.Failure("Bts.NotConfigured"));
    public BtsCalculateRequest? LastRequest { get; private set; }

    // ── CreateOrder ──
    public Result<BtsOrderData> CreateOrderResult { get; set; } =
        Result<BtsOrderData>.Failure(Error.Failure("Bts.NotConfigured"));
    public BtsCreateOrderRequest? LastCreateRequest { get; private set; }
    public int CreateOrderCallCount { get; private set; }

    // ── Sticker ──
    public Result<BtsStickerData> StickerResult { get; set; } =
        Result<BtsStickerData>.Failure(Error.Failure("Bts.NoSticker"));

    // ── Cancel ──
    public Result CancelResult { get; set; } = Result.Success();
    public long? LastCancelledOrderId { get; private set; }
    public int CancelCallCount { get; private set; }

    public Task<Result<BtsCalculateData>> CalculateAsync(BtsCalculateRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(CalculateResult);
    }

    public Task<Result<BtsOrderData>> CreateOrderAsync(BtsCreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        CreateOrderCallCount++;
        return Task.FromResult(CreateOrderResult);
    }

    public Task<Result<BtsStickerData>> GetStickerAsync(long orderId, CancellationToken cancellationToken = default)
        => Task.FromResult(StickerResult);

    public Task<Result> CancelOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        LastCancelledOrderId = orderId;
        CancelCallCount++;
        return Task.FromResult(CancelResult);
    }

    // ── Unused members ──
    public Task<Result> LoginAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result<List<BtsRegion>>> GetRegionsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<BtsRegion>>.Success([]));

    public Task<Result<List<BtsCity>>> GetCitiesAsync(string regionCode, bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<BtsCity>>.Success([]));

    public Task<Result<List<BtsBranch>>> GetBranchesAsync(string regionCode, string cityCode, bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<BtsBranch>>.Success([]));

    public Task<Result<List<BtsPackageType>>> GetPackageTypesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<BtsPackageType>>.Success([]));

    public Task<Result<List<BtsPostType>>> GetPostTypesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<BtsPostType>>.Success([]));

    public Task<Result<BtsOrderData>> EditOrderAsync(long orderId, BtsCreateOrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<BtsOrderData>.Success(new BtsOrderData()));

    public Task<Result<BtsOrderData>> GetOrderDetailAsync(long orderId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<BtsOrderData>.Success(new BtsOrderData()));

    public Task<Result<BtsTrackData>> TrackOrderAsync(long orderId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<BtsTrackData>.Success(new BtsTrackData()));
}
