namespace VendlyServer.Application.Jobs;

public static class JobsRegistrar
{
    public static void RegisterRecurringJobs()
    {
        // Register recurring jobs here using Hangfire
        // Example:
        // RecurringJob.AddOrUpdate<ISomeJob>(
        //     "job-id",
        //     job => job.ExecuteAsync(),
        //     "0 * * * *");
    }
}
