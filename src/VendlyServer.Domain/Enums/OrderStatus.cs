namespace VendlyServer.Domain.Enums;

public enum OrderStatus
{
    New, 
    Accepted, 
    Preparing, 
    Shipped, 
    InTransit,
    OutForDelivery, 
    Delivered, 
    Cancelled, 
    ReturnRequested, 
    Returned
}
