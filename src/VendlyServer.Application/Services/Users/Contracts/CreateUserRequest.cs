using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Users.Contracts;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Password,
    string? Email,
    UserRole Role);
