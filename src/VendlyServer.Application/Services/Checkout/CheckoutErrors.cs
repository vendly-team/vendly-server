using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Checkout;

public static class CheckoutErrors
{
    public static readonly Error OrderNotFound = Error.NotFound("Checkout.OrderNotFound");
    public static readonly Error NotDraft = Error.Validation("Checkout.NotDraft", "Order is not in draft status.");
    public static readonly Error PaymentUrlFailed = Error.Failure("Checkout.PaymentUrlFailed");
    public static readonly Error ProviderNotSupported = Error.Validation("Checkout.ProviderNotSupported", "Payment provider is not supported.");
}
