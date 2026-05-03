using Hangfire;
using Scalar.AspNetCore;
using VendlyServer.Api;
using VendlyServer.Api.Filters;
using VendlyServer.Application;
using VendlyServer.Application.Jobs;
using VendlyServer.Infrastructure;
using VendlyServer.Infrastructure.Extensions.Seed;
using VendlyServer.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .ConfigureApplication()
    .ConfigureInfrastructure(builder.Configuration)
    .ConfigureSwagger()
    .ConfigureControllers()
    .ConfigureCors(builder.Configuration)
    .ConfigureHangfire(builder.Configuration);

builder.ConfigureHostConfigurations();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Vendly Server API";
        options.InjectStylesheet("/swagger-ui/custom.css");
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

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(hangfireUser, hangfirePwd) }
});

app.MapControllers();

JobsRegistrar.RegisterRecurringJobs();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.SeedAsync();
}

// await app.ApplyMigrationsAsync();

app.Run();
