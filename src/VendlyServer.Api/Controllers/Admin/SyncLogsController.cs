using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Jobs.SmartupCatalog;
using VendlyServer.Application.Services.SyncLogs;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/sync-logs")]
[Authorize(Roles = "Admin,Manager")]
public class SyncLogsController(ISyncLogService syncLogService) : AdminController
{
    /// <summary>
    /// Get paginated sync logs.
    /// </summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync([FromQuery] DataQueryRequest request, CancellationToken ct = default)
    {
        var result = await syncLogService.GetAllAsync(request, ct);
        return Results.Ok(result);
    }

    /// <summary>
    /// Get sync log detail by id.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var result = await syncLogService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Trigger Smartup catalog sync job.
    /// </summary>
    [HttpPost("trigger")]
    public IResult TriggerSync([FromServices] IBackgroundJobClient jobs)
    {
        jobs.Enqueue<ISmartupCatalogSyncJob>(job => job.ExecuteAsync(CancellationToken.None));
        return Results.Accepted();
    }
}
