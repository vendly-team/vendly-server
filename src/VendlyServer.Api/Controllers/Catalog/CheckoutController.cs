using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/checkout")]
public class CheckoutController(ICheckoutService checkoutService) : AuthorizedController
{
    /// <summary>Create an order from the cart and return the Hamkorbank payment page URL.</summary>
    [HttpPost]
    public async Task<IResult> CreateAsync(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await checkoutService.CreateAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get payment/order status by order number.</summary>
    [HttpGet("status/{orderNumber}")]
    public async Task<IResult> GetStatusAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var result = await checkoutService.GetStatusByOrderAsync(UserId, orderNumber, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
