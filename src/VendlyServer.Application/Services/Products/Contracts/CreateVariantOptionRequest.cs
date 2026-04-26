using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Products.Contracts;

public record CreateVariantOptionRequest(
    string Name,
    IFormFile? Image,
    int? DisplayOrder = null);
