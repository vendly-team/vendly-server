using Hangfire;
using Scalar.AspNetCore;
using Serilog;
using VendlyServer.Api;
using VendlyServer.Api.Filters;
using VendlyServer.Application;
using VendlyServer.Application.Jobs;
using VendlyServer.Infrastructure;
using VendlyServer.Infrastructure.Extensions.Seed;
using VendlyServer.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Serilog: writes to console + Seq (configured via appsettings:Serilog).
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "VendlyServer")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

builder.Services
    .ConfigureApplication()
    .ConfigureInfrastructure(builder.Configuration)
    .ConfigureSwagger()
    .ConfigureExceptionHandler()
    .ConfigureControllers()
    .ConfigureCors()
    .ConfigureHangfire(builder.Configuration);

builder.ConfigureHostConfigurations();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Vendly Server API";
        options.InjectStylesheet("/swagger-ui/custom.css");
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Vendly API")
            .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var hangfireUser = app.Configuration["Hangfire:Dashboard:Username"]
    ?? throw new InvalidOperationException("Hangfire:Dashboard:Username is not configured.");
var hangfirePwd = app.Configuration["Hangfire:Dashboard:Password"]
    ?? throw new InvalidOperationException("Hangfire:Dashboard:Password is not configured.");

if (string.IsNullOrWhiteSpace(hangfireUser))
    throw new InvalidOperationException("Hangfire:Dashboard:Username must not be empty. Set it via the Hangfire__Dashboard__Username environment variable.");
if (string.IsNullOrWhiteSpace(hangfirePwd))
    throw new InvalidOperationException("Hangfire:Dashboard:Password must not be empty. Set it via the Hangfire__Dashboard__Password environment variable.");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(hangfireUser, hangfirePwd) }
});

app.MapControllers();

JobsRegistrar.RegisterRecurringJobs();

await app.ApplyMigrationsAsync();

app.Run();
