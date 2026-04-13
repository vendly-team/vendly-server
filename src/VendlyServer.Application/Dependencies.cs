using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VendlyServer.Application.Jobs.BtsCatalog;
using VendlyServer.Application.Services.BtsRef;

namespace VendlyServer.Application;

public static class Dependencies
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Dependencies).Assembly);

        // Services
        services.AddScoped<IBtsRefService, BtsRefService>();

        // Jobs
        services.AddScoped<IBtsCatalogSyncJob, BtsCatalogSyncJob>();

        return services;
    }
}
