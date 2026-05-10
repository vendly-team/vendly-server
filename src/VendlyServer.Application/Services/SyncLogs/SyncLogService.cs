using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.SyncLogs.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.SyncLogs;

public class SyncLogService(AppDbContext dbContext) : ISyncLogService
{
    public async Task<PagedList<SyncLogListItem>> GetAllAsync(DataQueryRequest request, CancellationToken ct = default)
    {
        var query = dbContext.SyncLogs
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.StartedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SyncLogListItem(
                x.Id,
                x.Source,
                (int)x.Status,
                x.TotalProcessed,
                x.CreatedCount,
                x.UpdatedCount,
                x.ErrorCount,
                x.DurationMs,
                x.StartedAt,
                x.FinishedAt,
                x.RequestUrl))
            .ToListAsync(ct);

        return new PagedList<SyncLogListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<Result<SyncLogDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var entity = await dbContext.SyncLogs
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .SingleOrDefaultAsync(ct);

        if (entity is null) return SyncLogErrors.NotFound;

        return new SyncLogDetailResponse(
            entity.Id,
            entity.Source,
            (int)entity.Status,
            entity.TotalProcessed,
            entity.CreatedCount,
            entity.UpdatedCount,
            entity.ErrorCount,
            entity.DurationMs,
            entity.StartedAt,
            entity.FinishedAt,
            entity.RequestUrl,
            entity.RequestBody?.RootElement.GetRawText(),
            entity.Response?.RootElement.GetRawText(),
            entity.ErrorDetail?.RootElement.GetRawText());
    }
}
