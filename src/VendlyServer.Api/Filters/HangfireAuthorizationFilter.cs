using Hangfire.Dashboard;

namespace VendlyServer.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
