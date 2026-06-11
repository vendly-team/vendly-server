using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Orders;

public static class OrderErrors
{
    public static readonly Error NotFound = Error.NotFound("Order.NotFound");
    public static readonly Error UnknownStatus = Error.Validation("Order.UnknownStatus", "Unknown order status.");
    public static readonly Error InvalidTransition = Error.Validation("Order.InvalidTransition", "This status transition is not allowed.");
    public static readonly Error NotPaid = Error.Validation("Order.NotPaid", "Order is not paid yet.");
    public static readonly Error NotCancellable = Error.Conflict("Order.NotCancellable");
    public static readonly Error ShippingFailed = Error.Failure("Order.ShippingFailed");
    public static readonly Error StickerNotAvailable = Error.NotFound("Order.StickerNotAvailable");
    public static readonly Error CartEmpty = Error.Validation("Order.CartEmpty", "Cart is empty.");
    public static readonly Error AddressNotFound = Error.NotFound("Order.AddressNotFound");
    public static readonly Error NotDraft = Error.Validation("Order.NotDraft", "Order is not in draft status.");
    public static readonly Error PaymentFailed = Error.Failure("Order.PaymentFailed");
}
