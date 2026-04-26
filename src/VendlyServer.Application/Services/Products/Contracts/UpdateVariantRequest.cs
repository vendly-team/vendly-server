using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Products.Contracts;

public record UpdateVariantRequest(
    string? Name,
    decimal Price,
    int Quantity,
    bool IsActive,
    IFormFile? Image);
