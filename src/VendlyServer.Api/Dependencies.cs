using System.Reflection;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using VendlyServer.Api.Filters;
using VendlyServer.Api.Middlewares;
using VendlyServer.Infrastructure.Extensions.Seed;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Api;

public static class Dependencies
{
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<EnumSchemaFilter>();

            // XML comments for Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            // JWT Bearer auth in Swagger
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter JWT token"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ModelValidationFilter>();
        });

        services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection ConfigureCors(
        this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
                else
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection ConfigureHangfire(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(
                        configuration.GetConnectionString("DefaultConnectionString"));
                });
        });

        services.AddHangfireServer();

        return services;
    }

    public static WebApplicationBuilder ConfigureHostConfigurations(this WebApplicationBuilder builder)
    {
        _ = builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory,
                $"appsettings.{builder.Environment.EnvironmentName}.json"),
            optional: false);
        _ = builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory,
                $"appsettings.json"),
            optional: false);
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Applies any pending database migrations to the application's database and seeds the database with initial data.
    /// </summary>
    /// <remarks>This method checks for pending migrations and applies them if any are found. After migrations
    /// are applied, the database is seeded with initial data. Ensure that the application's database context is
    /// properly configured before calling this method.</remarks>
    /// <param name="app">The web application instance whose service provider is used to resolve the database context and apply
    /// migrations.</param>
    /// <returns>A task that represents the asynchronous operation of applying migrations and seeding the database.</returns>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Check and apply pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            Console.WriteLine("Applying pending migrations...");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Migrations applied successfully.");
        }
        else
        {
            Console.WriteLine("No pending migrations found.");
        }

        await dbContext.SeedAsync();
    }
}
