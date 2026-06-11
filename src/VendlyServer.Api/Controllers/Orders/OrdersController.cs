using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;

namespace VendlyServer.Api.Controllers.Orders;

[Route("api/orders")]
public class OrdersController(IOrderService orderService, ICheckoutService checkoutService) : AuthorizedController
{
    /// <summary>Create a draft order from the current cart and set the delivery address.</summary>
    [HttpPost]
    public async Task<IResult> CreateDraftAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CreateDraftAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update the delivery address on a draft order.</summary>
    [HttpPatch("{id:long}/address")]
    public async Task<IResult> SetAddressAsync(
        long id,
        [FromBody] SetOrderAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.SetAddressAsync(UserId, id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Initiate payment for a draft order and return the Hamkorbank payment page URL.</summary>
    [HttpPost("{id:long}/payment")]
    public async Task<IResult> InitiatePaymentAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await checkoutService.InitiatePaymentAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Cancel one of the current user's unpaid (draft/new) orders.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> CancelDraftAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await orderService.CancelDraftAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>List the current user's active orders (draft/new + in-fulfillment).</summary>
    [HttpGet("active")]
    public async Task<IResult> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetActiveOrdersAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>List the current user's orders.</summary>
    [HttpGet]
    public async Task<IResult> GetMyAsync(
        [FromQuery] OrderFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetMyOrdersAsync(UserId, filter, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get one of the current user's orders by id.</summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetMyByIdAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
