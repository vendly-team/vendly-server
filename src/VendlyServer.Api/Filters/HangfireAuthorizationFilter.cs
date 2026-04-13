using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace VendlyServer.Api.Filters;

public class HangfireAuthorizationFilter(string username, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (authHeader != null && authHeader.StartsWith("Basic "))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(
                    Convert.FromBase64String(authHeader["Basic ".Length..].Trim()));
                var parts = decoded.Split(':', 2);
                if (parts.Length == 2)
                {
                    var usernameMatch = CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(parts[0]), Encoding.UTF8.GetBytes(username));
                    var passwordMatch = CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(parts[1]), Encoding.UTF8.GetBytes(password));
                    if (usernameMatch && passwordMatch)
                        return true;
                }
            }
            catch (FormatException) { }
        }

        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = 401;
        return false;
    }
}
