using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductPublicListResponse(
    long Id,
    MultiLanguageField Name,
    long CategoryId,
    MultiLanguageField CategoryName,
    decimal? MinPrice);
