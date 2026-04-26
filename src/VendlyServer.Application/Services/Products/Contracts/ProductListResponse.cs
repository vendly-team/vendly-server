using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductListResponse(
    long Id,
    long CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    SyncSource SyncSource,
    bool IsActive,
    int VariantCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
