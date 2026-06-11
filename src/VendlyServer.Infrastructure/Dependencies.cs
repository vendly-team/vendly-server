using Npgsql;
using Microsoft.Extensions.Configuration;
using VendlyServer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using VendlyServer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.Smartup;
using VendlyServer.Infrastructure.Brokers.Hamkor;
using VendlyServer.Infrastructure.Brokers.Cbu;

namespace VendlyServer.Infrastructure;

public static class Dependencies
{
    public static IServiceCollection ConfigureInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureDbContext(configuration)
            .ConfigureAuthentication()
            .ConfigureBtsExpress()
            .ConfigureSmartup()
            .ConfigureHamkor()
            .ConfigureCbuCurrency();

        return services;
    }

    private static IServiceCollection ConfigureDbContext(
        this IServiceCollection services, IConfiguration configuration)
    {
        var dataSource = new NpgsqlDataSourceBuilder(
                configuration.GetConnectionString("DefaultConnectionString"))
            .EnableDynamicJson()
            .Build();

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(dataSource)
                .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection ConfigureAuthentication(this IServiceCollection services)
    {
        services.ConfigureOptions<JwtOptionsSetup>();
        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    private static IServiceCollection ConfigureBtsExpress(this IServiceCollection services)
    {
        services.ConfigureOptions<BtsExpressOptionsSetup>();
        services.AddMemoryCache();
        services.AddHttpClient("BtsExpress");
        services.AddSingleton<IBtsBroker, BtsBroker>();

        return services;
    }

    private static IServiceCollection ConfigureSmartup(this IServiceCollection services)
    {
        services.ConfigureOptions<SmartupOptionsSetup>();
        services.AddHttpClient("Smartup");
        services.AddSingleton<ISmartupBroker, SmartupBroker>();

        return services;
    }

    private static IServiceCollection ConfigureHamkor(this IServiceCollection services)
    {
        services.ConfigureOptions<HamkorOptionsSetup>();
        services.AddHttpClient("Hamkor");
        services.AddSingleton<IHamkorBroker, HamkorBroker>();

        return services;
    }

    private static IServiceCollection ConfigureCbuCurrency(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient("CbuCurrency", client =>
        {
            client.BaseAddress = new Uri("https://cbu.uz/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddSingleton<ICbuCurrencyBroker, CbuCurrencyBroker>();

        return services;
    }
}
