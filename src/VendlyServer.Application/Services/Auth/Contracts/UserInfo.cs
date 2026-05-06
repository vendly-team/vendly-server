namespace VendlyServer.Application.Services.Auth.Contracts;

public record UserInfo(long Id, string FirstName, string LastName, string? Email, string Phone, string Role);
