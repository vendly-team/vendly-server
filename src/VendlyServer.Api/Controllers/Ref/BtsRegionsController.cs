using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Api.Controllers.Ref;

[Route("api/bts/regions")]
public class BtsRegionsController(IBtsRefService btsRefService) : AuthorizedController
{
    /// <summary>Get all BTS regions.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetAllRegionsAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get BTS region by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetRegionByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create BTS region.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] SaveBtsRegionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.AddRegionAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update BTS region.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] SaveBtsRegionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.UpdateRegionAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete BTS region.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.DeleteRegionAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
