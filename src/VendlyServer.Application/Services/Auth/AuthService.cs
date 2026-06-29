using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
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
    ISmsService smsService,
    IHostEnvironment environment) : IAuthService
{
    private const int OtpTtlMinutes = 5;
    private const int MaxOtpAttempts = 5;
    private const int MaxResendCount = 3;

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
            var smsResult = await SendOtpAsync(user.Phone, code, cancellationToken);
            if (smsResult.IsFailure) return smsResult.Error;

            var existingOtp = await dbContext.Otps
                .SingleOrDefaultAsync(o => o.Phone == user.Phone && o.Type == OtpType.Login, cancellationToken);

            if (existingOtp is not null)
                dbContext.Otps.Remove(existingOtp);

            var otp = new Otp
            {
                Phone = user.Phone,
                Code = code,
                Type = OtpType.Login,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                ResendCount = 0,
                Attempts = 0,
                SmsMessageId = smsResult.Data
            };

            dbContext.Otps.Add(otp);
            await dbContext.SaveChangesAsync(cancellationToken);

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

        var smsResult = await SendOtpAsync(request.Phone, code, cancellationToken);
        if (smsResult.IsFailure) return smsResult.Error;

        var user = new User
        {
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Phone        = request.Phone,
            Email        = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsVerified   = false,
            Role         = UserRole.Customer,
        };

        var otp = new Otp
        {
            Phone = request.Phone,
            Code = code,
            Type = OtpType.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
            ResendCount = 0,
            Attempts = 0,
            SmsMessageId = smsResult.Data
        };

        dbContext.Users.Add(user);
        dbContext.Otps.Add(otp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(request.Phone, OtpTtlMinutes * 60, "OTP code sent.");
    }

    public async Task<Result<AuthResponse>> VerifyRegistrationOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Phone == request.Phone && !u.IsDeleted && !u.IsVerified, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        var otp = await dbContext.Otps
            .SingleOrDefaultAsync(o => o.Phone == request.Phone && o.Type == OtpType.Registration, cancellationToken);

        if (otp is null)
            return AuthErrors.OtpExpired;

        if (otp.Code != request.Code)
        {
            otp.Attempts++;
            if (otp.Attempts >= MaxOtpAttempts)
                dbContext.Otps.Remove(otp);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthErrors.OtpInvalid;
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            dbContext.Otps.Remove(otp);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthErrors.OtpExpired;
        }

        user.IsVerified = true;
        dbContext.Otps.Remove(otp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> VerifyLoginOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var otp = await dbContext.Otps
            .SingleOrDefaultAsync(o => o.Phone == request.Phone && o.Type == OtpType.Login, cancellationToken);

        if (otp is null)
            return AuthErrors.OtpExpired;

        if (otp.Code != request.Code)
        {
            otp.Attempts++;
            if (otp.Attempts >= MaxOtpAttempts)
                dbContext.Otps.Remove(otp);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthErrors.OtpInvalid;
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            dbContext.Otps.Remove(otp);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthErrors.OtpExpired;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Phone == request.Phone && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            dbContext.Otps.Remove(otp);
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthErrors.UserNotFound;
        }

        user.IsVerified = true;
        dbContext.Otps.Remove(otp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<Result<RegisterResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Phone == request.Phone && !u.IsDeleted && !u.IsVerified, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        var otp = await dbContext.Otps
            .SingleOrDefaultAsync(o => o.Phone == request.Phone && o.Type == OtpType.Registration, cancellationToken);

        if (otp is null)
            return AuthErrors.OtpExpired;

        if (otp.ResendCount >= MaxResendCount)
            return AuthErrors.OtpResendLimitExceeded;

        var code = GenerateOtpCode();

        var smsResult = await SendOtpAsync(request.Phone, code, cancellationToken);
        if (smsResult.IsFailure) return smsResult.Error;

        otp.Code = code;
        otp.ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes);
        otp.ResendCount++;
        otp.Attempts = 0;
        otp.SmsMessageId = smsResult.Data;
        await dbContext.SaveChangesAsync(cancellationToken);

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

    private async Task<Result<long?>> SendOtpAsync(string phone, string code, CancellationToken cancellationToken)
    {
        // Dev/Stage'da real SMS yubormaymiz — kod baribir 555555.
        if (IsFixedCodeEnvironment)
            return (long?)null;

        var message = $"Vendly: tasdiqlash kodingiz {code}. Hech kimga bermang.";
        var result = await smsService.SendAsync(new SendSmsRequest { Phone = phone, Message = message }, cancellationToken);
        return result.IsSuccess ? result.Data.Id : result.Error;
    }

}
