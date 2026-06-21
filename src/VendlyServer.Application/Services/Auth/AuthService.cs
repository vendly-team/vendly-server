using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Application.Services.Sms;
using VendlyServer.Application.Services.Sms.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Auth;

public class AuthService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IOptions<JwtOptions> jwtOptions,
    IMemoryCache cache,
    ISmsService smsService,
    IHostEnvironment environment) : IAuthService
{
    private const int OtpTtlMinutes = 5;
    private const int MaxOtpAttempts = 5;

    // Dev/Stage'da OTP doim 555555 va real SMS yuborilmaydi.
    private bool IsFixedCodeEnvironment => environment.IsDevelopment() || environment.IsStaging();


    public async Task<Result<LoginResultResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => (u.Phone == request.Login || u.Email == request.Login) && !u.IsDeleted, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (user.IsBlocked)
            return AuthErrors.UserBlocked;

        if (!user.IsVerified)
        {
            var code = GenerateOtpCode();
            var send = await SendOtpAsync(user.Phone, code, cancellationToken);
            if (send.IsFailure) return send.Error;

            cache.Set(OtpKey(user.Phone), new OtpEntry
            {
                Code = code,
                FirstName = user.FirstName,
                LastName = user.LastName ?? string.Empty,
                Phone = user.Phone,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
            }, TimeSpan.FromMinutes(OtpTtlMinutes));

            var otpResponse = new RegisterResponse(user.Phone, OtpTtlMinutes * 60, "OTP code sent.");
            return new LoginResultResponse(Otp: otpResponse);
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
        return new LoginResultResponse(Auth: authResponse);
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Phone == request.Phone && !u.IsDeleted, cancellationToken);

        if (exists) return AuthErrors.UserAlreadyExists;

        var code = GenerateOtpCode();

        // OTP'ni yuboramiz (prod). Yuborib bo'lmasa — ro'yxatdan o'tkazmaymiz.
        var send = await SendOtpAsync(request.Phone, code, cancellationToken);
        if (send.IsFailure) return send.Error;

        // User hali yaratilmaydi — pending registration cache'da 5 daqiqa turadi.
        cache.Set(OtpKey(request.Phone), new OtpEntry
        {
            Code         = code,
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Phone        = request.Phone,
            Email        = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
        }, TimeSpan.FromMinutes(OtpTtlMinutes));

        return new RegisterResponse(request.Phone, OtpTtlMinutes * 60, "OTP code sent.");
    }

    public async Task<Result<AuthResponse>> VerifyRegistrationOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var key = OtpKey(request.Phone);

        if (!cache.TryGetValue(key, out OtpEntry? entry) || entry is null)
            return AuthErrors.OtpExpired;

        if (entry.Code != request.Code)
        {
            entry.Attempts++;
            if (entry.Attempts >= MaxOtpAttempts)
                cache.Remove(key);
            return AuthErrors.OtpInvalid;
        }

        cache.Remove(key);

        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Phone == entry.Phone && !u.IsDeleted, cancellationToken);

        if (exists) return AuthErrors.UserAlreadyExists;

        var user = new User
        {
            FirstName    = entry.FirstName,
            LastName     = entry.LastName,
            Phone        = entry.Phone,
            Email        = entry.Email,
            PasswordHash = entry.PasswordHash,
            Role         = UserRole.Customer,
            IsVerified   = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> VerifyLoginOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var key = OtpKey(request.Phone);

        if (!cache.TryGetValue(key, out OtpEntry? entry) || entry is null)
            return AuthErrors.OtpExpired;

        if (entry.Code != request.Code)
        {
            entry.Attempts++;
            if (entry.Attempts >= MaxOtpAttempts)
                cache.Remove(key);
            return AuthErrors.OtpInvalid;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Phone == request.Phone && !u.IsDeleted, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        cache.Remove(key);

        user.IsVerified = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<RegisterResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
    {
        var key = OtpKey(request.Phone);

        if (!cache.TryGetValue(key, out OtpEntry? entry) || entry is null)
            return AuthErrors.OtpExpired;

        var code = GenerateOtpCode();

        var send = await SendOtpAsync(request.Phone, code, cancellationToken);
        if (send.IsFailure) return send.Error;

        cache.Set(key, new OtpEntry
        {
            Code         = code,
            FirstName    = entry.FirstName,
            LastName     = entry.LastName,
            Phone        = entry.Phone,
            Email        = entry.Email,
            PasswordHash = entry.PasswordHash,
        }, TimeSpan.FromMinutes(OtpTtlMinutes));

        return new RegisterResponse(request.Phone, OtpTtlMinutes * 60, "OTP code resent.");
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var stored = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == tokenHash && !rt.IsDeleted, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return AuthErrors.InvalidRefreshToken;

        if (stored.User.IsDeleted)
            return AuthErrors.InvalidRefreshToken;

        if (stored.User.IsBlocked)
            return AuthErrors.UserBlocked;

        stored.IsRevoked = true;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var accessToken = jwtProvider.GenerateToken(stored.User.Id, stored.User.Email ?? string.Empty, stored.User.Role.ToString());
        var expiresIn = jwtOptions.Value.ExpirationInMinutes * 60;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId    = stored.User.Id,
            Token     = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, rawToken, expiresIn, ToUserInfo(stored.User));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == tokenHash && !rt.IsDeleted, cancellationToken);

        if (stored is null || stored.IsRevoked)
            return Result.Success();

        stored.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserInfo>> GetMeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? AuthErrors.UserNotFound : ToUserInfo(user);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = jwtProvider.GenerateToken(user.Id, user.Email ?? string.Empty, user.Role.ToString());
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresIn = jwtOptions.Value.ExpirationInMinutes * 60;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            Token     = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, rawToken, expiresIn, ToUserInfo(user));
    }

    private static string HashToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    private static UserInfo ToUserInfo(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Phone, user.Role.ToString().ToLowerInvariant());

    // === OTP ===

    // Dev/Stage → 555555; Production → kriptografik tasodifiy 6 xonali (000000–999999).
    private string GenerateOtpCode() =>
        IsFixedCodeEnvironment
            ? "555555"
            : RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private async Task<Result> SendOtpAsync(string phone, string code, CancellationToken cancellationToken)
    {
        // Dev/Stage'da real SMS yubormaymiz — kod baribir 555555.
        if (IsFixedCodeEnvironment)
            return Result.Success();

        var message = $"Vendly: tasdiqlash kodingiz {code}. Hech kimga bermang.";
        var result = await smsService.SendAsync(new SendSmsRequest { Phone = phone, Message = message }, cancellationToken);
        return result.IsSuccess ? Result.Success() : result.Error;
    }

    // Telefonni faqat raqamlarga normallashtirib kalit yasaymiz (register/verify mos kelishi uchun).
    private static string OtpKey(string phone) =>
        $"otp:reg:{new string(phone.Where(char.IsDigit).ToArray())}";

    // Pending registration — user OTP'ni tasdiqlamaguncha cache'da turadi (DB'ga yozilmaydi).
    private sealed class OtpEntry
    {
        public required string Code { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Phone { get; set; }
        public string? Email { get; set; }
        public required string PasswordHash { get; set; }
        public int Attempts { get; set; }
    }
}
