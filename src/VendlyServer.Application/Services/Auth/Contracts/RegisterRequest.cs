namespace VendlyServer.Application.Services.Auth.Contracts;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Password,
    string? Email);
