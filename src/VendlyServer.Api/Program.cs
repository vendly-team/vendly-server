using Hangfire;
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
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

JobsRegistrar.RegisterRecurringJobs();

app.Run();
