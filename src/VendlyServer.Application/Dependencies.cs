using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace VendlyServer.Application;

public static class Dependencies
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Dependencies).Assembly);

        // Register services here:
        // services.AddScoped<IDocumentService, DocumentService>();

        // Register jobs here:
        // services.AddScoped<ISomeJob, SomeJob>();

        return services;
    }
}
