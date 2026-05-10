namespace VendlyServer.Application.Services.Categories.Contracts;

public record CategoryResponse(
    long Id,
    string Name,
    string? Slug,
    string? ImageUrl,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
