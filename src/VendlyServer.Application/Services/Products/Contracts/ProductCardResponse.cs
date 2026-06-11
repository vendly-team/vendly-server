using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductCardResponse(
    long Id,
    MultiLanguageField Name,
    long CategoryId,
    MultiLanguageField CategoryName,
    string? Description,
    decimal? MinPrice,
    int TotalQuantity,
    int VariantCount,
    string? FirstImage,
    long? DefaultVariantId,
    long? FirstVariantId
);
