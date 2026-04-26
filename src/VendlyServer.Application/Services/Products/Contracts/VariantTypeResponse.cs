namespace VendlyServer.Application.Services.Products.Contracts;

public record VariantTypeResponse(
    long Id,
    long ProductId,
    string Name,
    int? DisplayOrder,
    List<VariantOptionResponse> Options);
