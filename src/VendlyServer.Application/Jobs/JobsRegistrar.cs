using Hangfire;
using VendlyServer.Application.Jobs.Auth;
using VendlyServer.Application.Jobs.BtsCatalog;
using VendlyServer.Application.Jobs.Payments;
using VendlyServer.Application.Jobs.SmartupCatalog;

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

        RecurringJob.AddOrUpdate<ICleanExpiredRefreshTokensJob>(
            "clean-expired-refresh-tokens",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(hour: 2));

        RecurringJob.AddOrUpdate<ISmartupCatalogSyncJob>(
            "smartup-catalog-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(hour: 0));

        // Click pending payment status polling — har 5 minutda Click Merchant API'dan tekshiradi.
        // Webhook miss bo'lganda "stuck pending" muammosini avtomatik hal qiladi.
        RecurringJob.AddOrUpdate<IClickStatusPollingJob>(
            "click-status-polling",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");
    }
}
