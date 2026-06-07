using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductSearchResponse(
    long Id,
    MultiLanguageField Name,
    decimal Price,
    int SkuCount,
    List<string> Images,
    bool IsAvailableForSale,
    string RedirectUrl);
