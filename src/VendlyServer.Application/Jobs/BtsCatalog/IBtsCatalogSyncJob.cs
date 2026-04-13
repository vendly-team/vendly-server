namespace VendlyServer.Application.Jobs.BtsCatalog;

public interface IBtsCatalogSyncJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
