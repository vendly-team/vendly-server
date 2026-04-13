namespace VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Enums;

public enum BtsOrderStatus
{
    AtSender = 100,
    CourierPickedUp = 200,
    AtSendingOffice = 300,
    InternalTransit = 400,
    AtSortingCenter = 500,
    InBag = 600,
    InTransit = 700,
    AtDeliveryOffice = 800,
    CourierDelivering = 1100,
    Delivered = 1200,
    Deleted = 1300,
    Expired = 1400
}
