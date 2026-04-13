using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Api.Controllers.Ref;

[Route("api/bts/package-types")]
public class BtsPackageTypesController(IBtsRefService btsRefService) : AuthorizedController
{
    /// <summary>Get all BTS package types.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetAllPackageTypesAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get BTS package type by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetPackageTypeByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create BTS package type.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.AddPackageTypeAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update BTS package type.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.UpdatePackageTypeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete BTS package type.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.DeletePackageTypeAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
