using System.Security.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VendlyServer.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception");

        // AuthenticationException is thrown by AuthorizedController when required JWT claims are missing.
        // UnauthorizedAccessException is intentionally excluded here: it is also raised by I/O APIs on
        // file system permission errors, so mapping it to 401 would mislead clients and hide 5xx failures.
        var (status, title) = exception switch
        {
            AuthenticationException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problemDetails = new ProblemDetails { Title = title, Status = status };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
