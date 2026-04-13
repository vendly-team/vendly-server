using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Api.Controllers.Ref;

[Route("api/bts/cities")]
public class BtsCitiesController(IBtsRefService btsRefService) : AuthorizedController
{
    /// <summary>Get all BTS cities.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetAllCitiesAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get BTS city by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.GetCityByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create BTS city.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] SaveBtsCityRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.AddCityAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update BTS city.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] SaveBtsCityRequest request, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.UpdateCityAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete BTS city.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await btsRefService.DeleteCityAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
