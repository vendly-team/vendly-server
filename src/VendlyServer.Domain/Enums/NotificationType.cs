namespace VendlyServer.Domain.Enums;

public enum NotificationType
{
    OrderPlaced, 
    OrderStatusChanged, 
    OrderDelivered,
    OrderCancelled, 
    ReturnApproved, 
    ReturnRejected,
    PaymentSucceeded, 
    PaymentFailed,
    TargetPromotion // Target habar huborganda ishlatiladi
}
