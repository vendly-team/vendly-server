using VendlyServer.Application.Services.Auth.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Auth;

public interface IAuthService
{
    Task<Result<LoginResultResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    // Register endi userni darrov yaratmaydi — OTP yuboradi (pending registration cache'da saqlanadi).
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    // Registration: OTP to'g'ri bo'lsa — yangi userni yaratadi va token qaytaradi.
    Task<Result<AuthResponse>> VerifyRegistrationOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);

    // Login: OTP to'g'ri bo'lsa — mavjud userni IsVerified = true qilib tokenini qaytaradi.
    Task<Result<AuthResponse>> VerifyLoginOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);

    // OTP'ni qayta yuborish (pending registration mavjud bo'lsa).
    Task<Result<RegisterResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> GetMeAsync(long userId, CancellationToken cancellationToken = default);
}
