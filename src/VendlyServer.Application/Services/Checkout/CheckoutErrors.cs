using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Checkout;

public static class CheckoutErrors
{
    public static readonly Error CartEmpty = Error.Validation("Checkout.CartEmpty", "Cart is empty.");
    public static readonly Error AddressNotFound = Error.NotFound("Checkout.AddressNotFound");
    public static readonly Error OrderNotFound = Error.NotFound("Checkout.OrderNotFound");
    public static readonly Error PaymentUrlFailed = Error.Failure("Checkout.PaymentUrlFailed");
}
