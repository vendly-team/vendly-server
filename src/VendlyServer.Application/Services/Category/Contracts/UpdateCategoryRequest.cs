using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Category.Contracts;

public record UpdateCategoryRequest(
    string Name,
    IFormFile? Image
);
