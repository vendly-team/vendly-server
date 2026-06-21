using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Authentication;

namespace VendlyServer.Tests.Controllers;

public class AuthControllerTests
{
    private readonly FakeAuthService _svc = new();

    private static readonly UserInfo SampleUser =
        new(1, "Alice", "A", "a@x.com", "111", "Customer");

    private static readonly AuthResponse SampleAuth =
        new("access", "refresh", 3600, SampleUser);

    private AuthController CreateController(long userId = 1)
    {
        var ctrl = new AuthController(_svc);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(CustomClaims.Id, userId.ToString())]))
            }
        };
        return ctrl;
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsOkWithData_OnSuccess()
    {
        _svc.LoginResult = Result<AuthResponse>.Success(SampleAuth);

        var result = await CreateController().LoginAsync(new LoginRequest("111", "pass"));

        var ok = Assert.IsType<Ok<AuthResponse>>(result);
        Assert.Equal("access", ok.Value!.AccessToken);
    }

    [Fact]
    public async Task Login_ReturnsProblem_OnInvalidCredentials()
    {
        _svc.LoginResult = Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);

        var result = await CreateController().LoginAsync(new LoginRequest("111", "bad"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsProblem_WhenUserBlocked()
    {
        _svc.LoginResult = Result<AuthResponse>.Failure(AuthErrors.UserBlocked);

        var result = await CreateController().LoginAsync(new LoginRequest("111", "pass"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Register ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ReturnsOkWithData_OnSuccess()
    {
        _svc.RegisterResult = Result<AuthResponse>.Success(SampleAuth);

        var result = await CreateController().RegisterAsync(
            new RegisterRequest("Alice", "A", "111", "pass", null));

        var ok = Assert.IsType<Ok<AuthResponse>>(result);
        Assert.Equal("refresh", ok.Value!.RefreshToken);
    }

    [Fact]
    public async Task Register_ReturnsProblem_WhenUserAlreadyExists()
    {
        _svc.RegisterResult = Result<AuthResponse>.Failure(AuthErrors.UserAlreadyExists);

        var result = await CreateController().RegisterAsync(
            new RegisterRequest("Alice", "A", "111", "pass", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── RefreshToken ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ReturnsOkWithData_OnSuccess()
    {
        _svc.RefreshResult = Result<AuthResponse>.Success(SampleAuth);

        var result = await CreateController().RefreshTokenAsync(new RefreshTokenRequest("tok"));

        Assert.IsType<Ok<AuthResponse>>(result);
    }

    [Fact]
    public async Task Refresh_ReturnsProblem_OnInvalidRefreshToken()
    {
        _svc.RefreshResult = Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);

        var result = await CreateController().RefreshTokenAsync(new RefreshTokenRequest("bad"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Logout ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ReturnsOk_OnSuccess()
    {
        _svc.LogoutResult = Result.Success();

        var result = await CreateController().LogoutAsync(new RefreshTokenRequest("tok"));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Logout_ReturnsProblem_OnInvalidRefreshToken()
    {
        _svc.LogoutResult = Result.Failure(AuthErrors.InvalidRefreshToken);

        var result = await CreateController().LogoutAsync(new RefreshTokenRequest("bad"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetMe ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetMeResult = Result<UserInfo>.Success(SampleUser);

        var result = await CreateController().GetMeAsync();

        var ok = Assert.IsType<Ok<UserInfo>>(result);
        Assert.Equal(1, ok.Value!.Id);
    }

    [Fact]
    public async Task GetMe_ReturnsProblem_WhenUserNotFound()
    {
        _svc.GetMeResult = Result<UserInfo>.Failure(AuthErrors.UserNotFound);

        var result = await CreateController().GetMeAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ──────────────────────────────────────────────────────────

    private class FakeAuthService : IAuthService
    {
        public Result<AuthResponse> LoginResult { get; set; } = Result<AuthResponse>.Success(SampleAuth);
        public Result<AuthResponse> RegisterResult { get; set; } = Result<AuthResponse>.Success(SampleAuth);
        public Result<AuthResponse> RefreshResult { get; set; } = Result<AuthResponse>.Success(SampleAuth);
        public Result LogoutResult { get; set; } = Result.Success();
        public Result<UserInfo> GetMeResult { get; set; } = Result<UserInfo>.Success(SampleUser);

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest r, CancellationToken ct = default) => Task.FromResult(LoginResult);
        public Task<Result<AuthResponse>> RegisterAsync(RegisterRequest r, CancellationToken ct = default) => Task.FromResult(RegisterResult);
        public Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest r, CancellationToken ct = default) => Task.FromResult(RefreshResult);
        public Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default) => Task.FromResult(LogoutResult);
        public Task<Result<UserInfo>> GetMeAsync(long userId, CancellationToken ct = default) => Task.FromResult(GetMeResult);
    }
}
