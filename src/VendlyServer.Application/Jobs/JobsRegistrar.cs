using Hangfire;
using VendlyServer.Application.Jobs.BtsCatalog;

namespace VendlyServer.Application.Jobs;

public static class JobsRegistrar
{
    public static void RegisterRecurringJobs()
    {
        // BTS catalog sync — runs every Sunday at 03:00 UTC
        RecurringJob.AddOrUpdate<IBtsCatalogSyncJob>(
            "bts-catalog-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Sunday, 3));
    }
}
