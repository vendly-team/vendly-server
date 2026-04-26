namespace VendlyServer.Application.Services.Users.Contracts;

public record UserReviewSummary(
    long Id,
    long ProductId,
    short Rating,
    string? Feedback,
    DateTime CreatedAt);
