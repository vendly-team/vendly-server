using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Checkout;

namespace VendlyServer.Api.Controllers.Catalog;

[Microsoft.AspNetCore.Mvc.Route("api/checkout")]
public class CheckoutController(ICheckoutService checkoutService) : AuthorizedController
{
    /// <summary>Get payment/order status by order number.</summary>
    [Microsoft.AspNetCore.Mvc.HttpGet("status/{orderNumber}")]
    public async Task<IResult> GetStatusAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var result = await checkoutService.GetStatusByOrderAsync(UserId, orderNumber, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
