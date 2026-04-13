using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress;

public static class BtsErrors
{
    public static readonly Error LoginFailed = Error.Failure("Bts.Login.Failed");
    public static readonly Error RefreshFailed = Error.Failure("Bts.Refresh.Failed");
    public static readonly Error TokenEmpty = Error.Failure("Bts.Token.Empty");

    public static readonly Error CreateOrderFailed = Error.Failure("Bts.CreateOrder.Failed");
    public static readonly Error EditOrderFailed = Error.Failure("Bts.EditOrder.Failed");
    public static readonly Error GetOrderDetailFailed = Error.Failure("Bts.GetOrderDetail.Failed");
    public static readonly Error CancelOrderFailed = Error.Failure("Bts.CancelOrder.Failed");
    public static readonly Error OrderAlreadyCancelled = Error.Conflict("Bts.CancelOrder.AlreadyCancelled");

    public static readonly Error TrackOrderFailed = Error.Failure("Bts.TrackOrder.Failed");
    public static readonly Error GetStickerFailed = Error.Failure("Bts.GetSticker.Failed");

    public static readonly Error CalculateFailed = Error.Failure("Bts.Calculate.Failed");

    public static readonly Error GetRegionsFailed = Error.Failure("Bts.GetRegions.Failed");
    public static readonly Error GetCitiesFailed = Error.Failure("Bts.GetCities.Failed");
    public static readonly Error GetBranchesFailed = Error.Failure("Bts.GetBranches.Failed");
    public static readonly Error GetPackageTypesFailed = Error.Failure("Bts.GetPackageTypes.Failed");
    public static readonly Error GetPostTypesFailed = Error.Failure("Bts.GetPostTypes.Failed");
}
