namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductVariantResponse(
    long Id,
    long ProductId,
    string? Name,
    decimal Price,
    int Quantity,
    bool IsActive,
    List<string> Images,
    List<VariantCombinationItem> Combination);
