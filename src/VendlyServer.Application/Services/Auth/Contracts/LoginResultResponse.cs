namespace VendlyServer.Application.Services.Auth.Contracts;

public record LoginResultResponse(
    AuthResponse? Auth = null,
    RegisterResponse? Otp = null);
