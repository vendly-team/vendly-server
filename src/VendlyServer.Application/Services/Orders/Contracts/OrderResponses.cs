namespace VendlyServer.Application.Services.Orders.Contracts;

public record OrderResponse(
    long Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string DeliveryStatus,
    decimal Subtotal,
    decimal DeliveryCost,
    decimal DiscountAmount,
    decimal TotalAmount,
    OrderDeliveryResponse Delivery,
    OrderPaymentResponse? Payment,
    List<OrderItemResponse> Items,
    List<OrderStatusHistoryResponse> StatusHistory,
    List<OrderNoteResponse> Notes,
    string? CustomerName,
    DateTime CreatedAt);

public record OrderItemResponse(
    long Id,
    long? ProductId,
    string ProductName,
    string Sku,
    string Image,
    int Qty,
    decimal Price,
    decimal Total);

public record OrderStatusHistoryResponse(string Status, string? Note, DateTime CreatedAt);

public record OrderNoteResponse(long Id, string Note, DateTime CreatedAt);

public record OrderDeliveryResponse(
    string City,
    string District,
    string Street,
    string House,
    string? Extra,
    string BtsCityCode,
    string? BtsBarcode,
    string? BtsTrackingUrl,
    string? BtsStickerUrl,
    string? BtsLastStatusName,
    DateTime? DeliveredAt);

public record OrderPaymentResponse(string Provider, string Status, decimal Amount, DateTime? PaidAt);

public record CreateOrderResponse(long Id, string OrderNumber);

public record OrderListItemResponse(
    long Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string DeliveryStatus,
    decimal Subtotal,
    decimal DeliveryCost,
    decimal TotalAmount,
    int ItemCount,
    string DeliveryCity,
    List<OrderItemResponse> Items,
    string? CustomerName,
    DateTime CreatedAt);
