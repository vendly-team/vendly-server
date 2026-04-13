using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Api.Controllers.Ref;

[Route("api/bts/post-types")]
public class BtsPostTypesController(IBtsRefService btsRefService) : AuthorizedController
{
    /// <summary>Get all BTS post types.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetAllPostTypesAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get BTS post type by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetPostTypeByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create BTS post type.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.AddPostTypeAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update BTS post type.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.UpdatePostTypeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete BTS post type.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.DeletePostTypeAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
