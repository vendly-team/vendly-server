using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Categories.Contracts;

public record UpdateCategoryRequest(
    string Name,
    IFormFile? Image
);
