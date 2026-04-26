using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Users.Contracts;

public record UserDetailResponse(
    long Id,
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    UserRole Role,
    bool IsBlocked,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<UserOrderSummary> Orders,
    List<UserReviewSummary> Reviews);
