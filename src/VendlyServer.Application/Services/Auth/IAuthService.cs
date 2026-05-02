using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> GetMeAsync(long userId, CancellationToken cancellationToken = default);
}
