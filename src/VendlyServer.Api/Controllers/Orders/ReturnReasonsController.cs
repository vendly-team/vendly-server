using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Infrastructure.Extensions;
using VendlyServer.Application.Services.ReturnReasons;
using VendlyServer.Application.Services.ReturnReasons.Contracts;

namespace VendlyServer.Api.Controllers.Orders;

[ApiController]
[Route("api/return-reasons")]
public class ReturnReasonsController(IReturnReasonService returnReasonService) : AdminController
{
    /// <summary>
    /// Get all return reasons.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync([FromQuery] ReturnReasonFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var result = await returnReasonService.GetAllAsync(filter, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get return reason by id.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await returnReasonService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Create new return reason.
    /// </summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] CreateReturnReasonRequest request, CancellationToken cancellationToken = default)
    {
        var result = await returnReasonService.AddAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Update return reason.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] CreateReturnReasonRequest request, CancellationToken cancellationToken = default)
    {
        var result = await returnReasonService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Delete return reason.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await returnReasonService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
