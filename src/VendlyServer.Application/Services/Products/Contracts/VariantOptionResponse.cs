namespace VendlyServer.Application.Services.Products.Contracts;

public record VariantOptionResponse(
    long Id,
    long VariantTypeId,
    string Name,
    string? ImageUrl,
    int? DisplayOrder);
