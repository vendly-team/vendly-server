using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.RecentlyViewed;
using VendlyServer.Application.Services.RecentlyViewed.Contracts;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/recently-viewed")]
public class RecentlyViewedController(IRecentlyViewedService recentlyViewedService) : AuthorizedController
{
    /// <summary>Get the current user's recently viewed products (latest first, max 20).</summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await recentlyViewedService.GetAllAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Track a product view for the current user.</summary>
    [HttpPost]
    public async Task<IResult> TrackAsync([FromBody] TrackProductViewRequest request, CancellationToken cancellationToken = default)
    {
        var result = await recentlyViewedService.TrackAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Bulk sync guest recently-viewed products to the current user (use after login).</summary>
    [HttpPost("sync")]
    public async Task<IResult> SyncAsync([FromBody] BulkSyncRecentlyViewedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await recentlyViewedService.BulkSyncAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Clear all recently viewed products for the current user.</summary>
    [HttpDelete]
    public async Task<IResult> ClearAsync(CancellationToken cancellationToken = default)
    {
        var result = await recentlyViewedService.ClearAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
