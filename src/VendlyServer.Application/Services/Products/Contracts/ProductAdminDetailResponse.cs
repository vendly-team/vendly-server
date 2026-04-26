using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductAdminDetailResponse(
    long Id,
    long CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    SyncSource SyncSource,
    bool IsActive,
    List<VariantTypeResponse> VariantTypes,
    List<ProductVariantResponse> Variants,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
