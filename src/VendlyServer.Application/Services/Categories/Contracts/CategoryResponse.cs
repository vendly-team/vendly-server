using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Categories.Contracts;

public record CategoryResponse(
    long Id,
    MultiLanguageField Name,
    string? Slug,
    string? ImageUrl,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
