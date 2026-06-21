namespace VendlyServer.Application.Services.Auth.Contracts;

// Register endi darrov token bermaydi — OTP yuboriladi, keyin verify-otp token qaytaradi.
public record RegisterResponse(
    string Phone,
    int ExpiresInSeconds,
    string Message);
