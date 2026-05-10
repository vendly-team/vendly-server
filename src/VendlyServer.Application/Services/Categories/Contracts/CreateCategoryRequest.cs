using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Categories.Contracts;

public record CreateCategoryRequest(
    string Name,
    IFormFile? Image
);
