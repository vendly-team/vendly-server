namespace VendlyServer.Application.Services.Users.Contracts;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? Email);
