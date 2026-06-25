using Hangfire;
using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.SmartupSync;

namespace VendlyServer.Application.Jobs.SmartupCatalog;

[DisableConcurrentExecution(timeoutInSeconds: 0)]
public class SmartupCatalogSyncJob(
    ISmartupSyncService smartupSyncService,
    ILogger<SmartupCatalogSyncJob> logger) : ISmartupCatalogSyncJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Smartup Catalog Sync job triggered");

        var result = await smartupSyncService.SyncAsync(cancellationToken);

        if (result.IsFailure)
            logger.LogError("Smartup Catalog Sync job failed: {Error}", result.Error.Code);
    }
}
