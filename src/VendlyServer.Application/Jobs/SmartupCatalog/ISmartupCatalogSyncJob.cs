namespace VendlyServer.Application.Jobs.SmartupCatalog;

public interface ISmartupCatalogSyncJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
