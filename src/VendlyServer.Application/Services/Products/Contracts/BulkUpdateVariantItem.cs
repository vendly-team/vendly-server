using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Products.Contracts;

public record BulkUpdateVariantItem(
    long Id,
    string? Name,
    decimal Price,
    int Quantity,
    bool IsActive,
    IFormFile? Image);
