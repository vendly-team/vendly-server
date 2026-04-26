using Microsoft.AspNetCore.Mvc;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Login with phone and password.</summary>
    [HttpPost("login")]
    public async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register")]
    public async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get new access and refresh tokens using a valid refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Revoke the provided refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
