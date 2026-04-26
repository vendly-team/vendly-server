using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Category.Contracts;

public record CreateCategoryRequest(
    string Name,
    IFormFile? Image
);
