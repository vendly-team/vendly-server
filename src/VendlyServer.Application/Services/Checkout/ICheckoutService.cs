using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;
using VendlyServer.Application.Services.Checkout.Contracts;

namespace VendlyServer.Application.Services.Checkout;

public interface ICheckoutService
{
    Task<Result<CheckoutResponse>> InitiatePaymentAsync(long userId, long orderId, PaymentProvider provider, CancellationToken cancellationToken = default);

    Task<Result> HandleCallbackAsync(HamkorCallbackRequest callback, CancellationToken cancellationToken = default);

    Task<Result<CheckoutStatusResponse>> GetStatusByOrderAsync(long userId, string orderNumber, CancellationToken cancellationToken = default);
}
