using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;

namespace VendlyServer.Api.Controllers.Orders;

[Route("api/orders")]
public class OrdersController(IOrderService orderService) : AuthorizedController
{
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
