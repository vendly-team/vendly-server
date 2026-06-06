using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Enums;

namespace VendlyServer.Application.Services.Shipping;

/// <summary>
/// Maps a BTS Express status code to our internal DeliveryStatus and (optionally) OrderStatus.
/// Grounded in <see cref="BtsOrderStatus"/>. Unknown codes leave the order status untouched
/// but the raw status is always recorded on the order/webhook event.
/// </summary>
public static class BtsStatusMapper
{
    public static (DeliveryStatus Delivery, OrderStatus? Order) Map(int code)
    {
        if (!Enum.IsDefined(typeof(BtsOrderStatus), code))
            return (DeliveryStatus.Unknown, null);

        return (BtsOrderStatus)code switch
        {
            BtsOrderStatus.AtSender
                => (DeliveryStatus.Pending, OrderStatus.Shipped),

            BtsOrderStatus.CourierPickedUp
                or BtsOrderStatus.AtSendingOffice
                or BtsOrderStatus.InternalTransit
                or BtsOrderStatus.AtSortingCenter
                or BtsOrderStatus.InBag
                or BtsOrderStatus.InTransit
                or BtsOrderStatus.AtDeliveryOffice
                => (DeliveryStatus.InTransit, OrderStatus.InTransit),

            BtsOrderStatus.CourierDelivering
                => (DeliveryStatus.OutForDelivery, OrderStatus.OutForDelivery),

            BtsOrderStatus.Delivered
                => (DeliveryStatus.Delivered, OrderStatus.Delivered),

            BtsOrderStatus.Deleted or BtsOrderStatus.Expired
                => (DeliveryStatus.Failed, null),

            _ => (DeliveryStatus.Unknown, null)
        };
    }
}
