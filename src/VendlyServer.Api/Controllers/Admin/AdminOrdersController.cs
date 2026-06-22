using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;

namespace VendlyServer.Api.Controllers.Admin;

[Route("api/admin/orders")]
[Authorize(Roles = "Admin,Manager")]
public class AdminOrdersController(IOrderService orderService) : AuthorizedController
{
    /// <summary>List all orders (admin), with optional status and search filters.</summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync(
        [FromQuery] OrderFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetAllAsync(filter, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get an order by id (admin) with full detail.</summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Advance an order's status. Moving to "Shipped" creates the BTS delivery.</summary>
    [HttpPut("{id:long}/status")]
    public async Task<IResult> UpdateStatusAsync(
        long id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.UpdateStatusAsync(UserId, id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Add an internal note to an order.</summary>
    [HttpPost("{id:long}/notes")]
    public async Task<IResult> AddNoteAsync(
        long id,
        [FromBody] AddOrderNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.AddNoteAsync(UserId, id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Cancel an order (and its BTS delivery if shipped).</summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<IResult> CancelAsync(
        long id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CancelAsync(UserId, Role.ToString(), id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get the BTS sticker URL/label for an order.</summary>
    [HttpGet("{id:long}/sticker")]
    public async Task<IResult> GetStickerAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetStickerAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(new { sticker = result.Data }) : result.ToProblemDetails();
    }
}
