using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.SmartupSync;

public interface ISmartupSyncService
{
    Task<Result> SyncAsync(CancellationToken cancellationToken = default);
}
