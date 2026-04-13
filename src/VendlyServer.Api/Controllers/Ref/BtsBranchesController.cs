using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Api.Controllers.Ref;

[Route("api/bts/branches")]
public class BtsBranchesController(IBtsRefService btsRefService) : AuthorizedController
{
    /// <summary>Get all BTS branches.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetAllBranchesAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get BTS branch by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetBranchByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create BTS branch.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] SaveBtsBranchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.AddBranchAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update BTS branch.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] SaveBtsBranchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.UpdateBranchAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete BTS branch.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.DeleteBranchAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
