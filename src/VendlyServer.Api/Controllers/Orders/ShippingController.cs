using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;

namespace VendlyServer.Api.Controllers.Orders;

[Route("api/shipping")]
public class ShippingController(IShippingCalculatorService shippingCalculatorService) : AuthorizedController
{
    /// <summary>Calculate the BTS delivery cost for a receiver city and parcel weight.</summary>
    [HttpPost("quote")]
    public async Task<IResult> QuoteAsync(
        [FromBody] ShippingQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await shippingCalculatorService.CalculateAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
