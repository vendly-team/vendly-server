using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new AuthService(
            _db,
            new StubPasswordHasher(),
            new StubJwtProvider(),
            Options.Create(new JwtOptions { ExpirationInMinutes = 60 }));

        _db.Users.Add(new User
        {
            Id = 1,
            FirstName = "Ali",
            LastName = "Valiyev",
            Phone = "+998901234567",
            Email = "ali@test.com",
            PasswordHash = "hash:secret",
            Role = UserRole.Customer
        });
        _db.SaveChanges();
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsToken_WhenPhoneAndPasswordCorrect()
    {
        var result = await _service.LoginAsync(new LoginRequest("+998901234567", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Data!.AccessToken);
        Assert.Equal("+998901234567", result.Data.User.Phone);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenEmailAndPasswordCorrect()
    {
        var result = await _service.LoginAsync(new LoginRequest("ali@test.com", "secret"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data!.RefreshToken);
    }

    [Fact]
    public async Task Login_ReturnsInvalidCredentials_WhenPasswordWrong()
    {
        var result = await _service.LoginAsync(new LoginRequest("+998901234567", "wrong"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_ReturnsInvalidCredentials_WhenUserNotFound()
    {
        var result = await _service.LoginAsync(new LoginRequest("noone@x.com", "secret"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_ReturnsUserBlocked_WhenUserIsBlocked()
    {
        _db.Users.Add(new User
        {
            Id = 2,
            FirstName = "Blocked",
            LastName = "User",
            Phone = "+998999999999",
            PasswordHash = "hash:pass",
            IsBlocked = true,
            Role = UserRole.Customer
        });
        await _db.SaveChangesAsync();

        var result = await _service.LoginAsync(new LoginRequest("+998999999999", "pass"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserBlocked, result.Error);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_CreatesUserAndReturnsToken()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Bob", "Smith", "+998700000001", "pass123", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Data!.AccessToken);
        Assert.True(await _db.Users.AnyAsync(u => u.Phone == "+998700000001"));
    }

    [Fact]
    public async Task Register_ReturnsUserAlreadyExists_WhenPhoneTaken()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Ali2", "V", "+998901234567", "pass", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserAlreadyExists, result.Error);
    }

    // ── RefreshTokenAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ReturnsNewTokens_WhenValid()
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = 1, UserId = 1, Token = "valid-refresh",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await _db.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest("valid-refresh"));

        Assert.True(result.IsSuccess);
        Assert.NotEqual("valid-refresh", result.Data!.RefreshToken);
        Assert.True(_db.RefreshTokens.Single(r => r.Token == "valid-refresh").IsRevoked);
    }

    [Fact]
    public async Task RefreshToken_ReturnsInvalidRefreshToken_WhenTokenNotFound()
    {
        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest("nonexistent"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task RefreshToken_ReturnsInvalidRefreshToken_WhenExpired()
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = 2, UserId = 1, Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest("expired-token"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task RefreshToken_ReturnsInvalidRefreshToken_WhenAlreadyRevoked()
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = 3, UserId = 1, Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(10), IsRevoked = true
        });
        await _db.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest("revoked-token"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, result.Error);
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_RevokesToken_WhenFound()
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = 10, UserId = 1, Token = "logout-token",
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        });
        await _db.SaveChangesAsync();

        var result = await _service.LogoutAsync("logout-token");

        Assert.True(result.IsSuccess);
        Assert.True(_db.RefreshTokens.Single(r => r.Token == "logout-token").IsRevoked);
    }

    [Fact]
    public async Task Logout_ReturnsSuccess_WhenTokenNotFound()
    {
        var result = await _service.LogoutAsync("nonexistent-token");

        Assert.True(result.IsSuccess);
    }

    // ── GetMeAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ReturnsUserInfo_WhenUserExists()
    {
        var result = await _service.GetMeAsync(userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.Id);
        Assert.Equal("Ali", result.Data.FirstName);
    }

    [Fact]
    public async Task GetMe_ReturnsUserNotFound_WhenUserMissing()
    {
        var result = await _service.GetMeAsync(userId: 999);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => hash == $"hash:{password}";
    }

    private sealed class StubJwtProvider : IJwtProvider
    {
        public string GenerateToken(long userId, string email, string role) => "test-token";
    }
}
