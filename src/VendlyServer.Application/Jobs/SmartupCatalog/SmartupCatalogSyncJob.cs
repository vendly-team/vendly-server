using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.SmartupSync;

namespace VendlyServer.Application.Jobs.SmartupCatalog;

public class SmartupCatalogSyncJob(
    ISmartupSyncService smartupSyncService,
    ILogger<SmartupCatalogSyncJob> logger) : ISmartupCatalogSyncJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Smartup Catalog Sync job triggered");

        var result = await smartupSyncService.SyncAsync(cancellationToken);

        if (result.IsFailure)
            logger.LogError(
                "Smartup Catalog Sync job failed: [{Code}] {Message}",
                result.Error.Code,
                result.Error.Message);
    }
}
