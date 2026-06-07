using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductCardResponse(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    string? Description,
    decimal? MinPrice,
    int TotalQuantity,
    int VariantCount,
    string? FirstImage,
    long? DefaultVariantId,
    long? FirstVariantId
);
