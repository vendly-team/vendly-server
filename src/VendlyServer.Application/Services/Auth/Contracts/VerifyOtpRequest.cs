namespace VendlyServer.Application.Services.Auth.Contracts;

public record VerifyOtpRequest(string Phone, string Code);
