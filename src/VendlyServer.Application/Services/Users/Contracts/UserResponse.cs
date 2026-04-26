using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Users.Contracts;

public record UserResponse(
    long Id,
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    UserRole Role,
    bool IsBlocked,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
