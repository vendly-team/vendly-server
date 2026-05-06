namespace VendlyServer.Application.Services.Categories.Contracts;

public record CategoryResponse(
    long Id,
    string Name,
    string? ImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
