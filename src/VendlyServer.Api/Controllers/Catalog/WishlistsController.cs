using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Wishlist;
using VendlyServer.Application.Services.Wishlist.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/wishlists")]
public class WishlistsController(IWishlistService wishlistService) : AuthorizedController
{
    /// <summary>Get all wishlist items for the current user.</summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.GetAllAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get wishlist item by id.</summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.GetByIdAsync(id, UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Add product to wishlist.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] AddWishlistRequest request, CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.AddAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Remove product from wishlist.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.DeleteAsync(id, UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
