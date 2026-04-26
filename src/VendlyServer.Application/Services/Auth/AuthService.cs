using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Auth;

public class AuthService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider) : IAuthService
{
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => (u.Phone == request.Login || u.Email == request.Login) && !u.IsDeleted, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (user.IsBlocked)
            return AuthErrors.UserBlocked;

        return await CreateAuthResponseAsync(user.Id, user.Email, user.Role, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Phone == request.Phone && !u.IsDeleted, cancellationToken);

        if (exists) return AuthErrors.UserAlreadyExists;

        var user = new User
        {
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Phone        = request.Phone,
            Email        = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role         = UserRole.Customer
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user.Id, user.Email, user.Role, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsDeleted, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return AuthErrors.InvalidRefreshToken;

        if (stored.User.IsBlocked)
            return AuthErrors.UserBlocked;

        stored.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(stored.User.Id, stored.User.Email, stored.User.Role, cancellationToken);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsDeleted, cancellationToken);

        if (stored is null || stored.IsRevoked)
            return AuthErrors.InvalidRefreshToken;

        stored.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        long userId, string? email, UserRole role, CancellationToken cancellationToken)
    {
        var accessToken = jwtProvider.GenerateToken(userId, email ?? string.Empty, role.ToString());

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId    = userId,
            Token     = rawToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, rawToken);
    }
}
