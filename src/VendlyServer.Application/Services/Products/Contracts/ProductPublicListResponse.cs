namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductPublicListResponse(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    decimal? MinPrice);
