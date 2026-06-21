using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.Auth.Contracts;

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
        if (result.IsFailure) return result.ToProblemDetails();

        var loginResult = result.Data;
        if (loginResult.Auth != null) return Results.Ok(loginResult.Auth);
        if (loginResult.Otp != null) return Results.Ok(loginResult.Otp);

        return result.ToProblemDetails();
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

    /// <summary>Verify OTP for registration or login — auto-detects the flow.</summary>
    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<IResult> VerifyOtpAsync(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.VerifyLoginOtpAsync(request, cancellationToken);
        if (result.IsSuccess)
            return Results.Ok(result.Data);

        // Agar login OTP bo'lmasa (user topilmasa) — registration OTP deb o'ylamiz.
        var registrationResult = await authService.VerifyRegistrationOtpAsync(request, cancellationToken);
        return registrationResult.IsSuccess ? Results.Ok(registrationResult.Data) : registrationResult.ToProblemDetails();
    }

    /// <summary>Resend the registration OTP code.</summary>
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    public async Task<IResult> ResendOtpAsync(
        [FromBody] ResendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.ResendOtpAsync(request, cancellationToken);
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
