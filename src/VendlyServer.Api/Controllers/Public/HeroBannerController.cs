using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.HeroBanners;
using VendlyServer.Application.Services.HeroBanners.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[Route("api/hero-banners")]
public class HeroBannerController(IHeroBannerService heroBannerService) : AuthorizedController
{
    /// <summary>Storefront — only active banners, sorted by SortOrder.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.GetActiveAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Admin — all banners (including inactive).</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get a single banner for the edit form.</summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create a new banner. Accepts an image file.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> CreateAsync(
        [FromForm] CreateHeroBannerRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update an existing banner. Accepts an optional new image.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromForm] UpdateHeroBannerRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Soft-delete a banner and remove its image from storage.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Toggle the IsActive flag without editing the banner.</summary>
    [HttpPatch("{id:long}/toggle")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await heroBannerService.ToggleActiveAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
