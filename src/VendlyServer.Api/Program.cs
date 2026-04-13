using Hangfire;
using Scalar.AspNetCore;
using VendlyServer.Api;
using VendlyServer.Api.Filters;
using VendlyServer.Application;
using VendlyServer.Application.Jobs;
using VendlyServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .ConfigureApplication()
    .ConfigureInfrastructure(builder.Configuration)
    .ConfigureSwagger()
    .ConfigureControllers()
    .ConfigureCors()
    .ConfigureHangfire(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
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

app.Run();
