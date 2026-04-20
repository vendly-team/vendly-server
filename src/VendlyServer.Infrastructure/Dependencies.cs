using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Infrastructure;

public static class Dependencies
{
    public static IServiceCollection ConfigureInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureDbContext(configuration)
            .ConfigureAuthentication()
            .ConfigureBtsExpress();

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
}
