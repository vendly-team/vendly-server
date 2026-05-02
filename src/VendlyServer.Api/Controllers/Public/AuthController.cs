using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Public;

[Route("api/auth")]
public class AuthController(IAuthService authService) : AuthorizedController
{
    /// <summary>Login with phone/email and password.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Register a new customer account.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get new access and refresh tokens using a valid refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Revoke the provided refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Get lightweight auth identity (id, name, phone, role) for the current user.
    /// Use this to hydrate client auth state after login or token refresh.
    /// For the full profile including orders and reviews, use GET /api/me.
    /// </summary>
    [HttpGet("me")]
    public async Task<IResult> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMeAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
