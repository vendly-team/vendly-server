using VendlyServer.Application.Services.SyncLogs.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.SyncLogs;

public interface ISyncLogService
{
    Task<PagedList<SyncLogListItem>> GetAllAsync(DataQueryRequest request, CancellationToken ct = default);
    Task<Result<SyncLogDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default);
}
