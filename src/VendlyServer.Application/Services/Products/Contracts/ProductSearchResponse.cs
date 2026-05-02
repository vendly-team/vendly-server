namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductSearchResponse(
    long Id,
    string Name,
    decimal Price,
    int SkuCount,
    List<string> Images,
    bool IsAvailableForSale,
    string RedirectUrl);
