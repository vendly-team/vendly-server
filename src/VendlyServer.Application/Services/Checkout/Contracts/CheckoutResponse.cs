namespace VendlyServer.Application.Services.Checkout.Contracts;

public record CheckoutResponse(string PaymentUrl, string OrderNumber);

public record CheckoutStatusResponse(string OrderNumber, string PaymentStatus, string OrderStatus);
