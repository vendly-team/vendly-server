namespace VendlyServer.Application.Services.Auth.Contracts;

public record AuthResponse(string AccessToken, string RefreshToken, UserInfo User);
