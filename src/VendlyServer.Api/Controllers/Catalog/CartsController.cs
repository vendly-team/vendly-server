using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Carts;
using VendlyServer.Application.Services.Carts.Contracts;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/carts")]
public class CartsController(ICartService cartService) : AuthorizedController
{
    /// <summary>Get or create the current user's cart.</summary>
    [HttpGet]
    public async Task<IResult> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var result = await cartService.GetOrCreateAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Add item to cart.</summary>
    [HttpPost("items")]
    public async Task<IResult> AddItemAsync(
        [FromBody] CartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await cartService.AddItemAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update cart item quantity.</summary>
    [HttpPut("items/{id:long}")]
    public async Task<IResult> UpdateItemAsync(
        long id,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await cartService.UpdateItemAsync(UserId, id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Remove item from cart.</summary>
    [HttpDelete("items/{id:long}")]
    public async Task<IResult> RemoveItemAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await cartService.RemoveItemAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Clear all items from cart.</summary>
    [HttpDelete]
    public async Task<IResult> ClearAsync(CancellationToken cancellationToken = default)
    {
        var result = await cartService.ClearAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
