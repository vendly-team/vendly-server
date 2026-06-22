using Microsoft.AspNetCore.Http;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Categories.Contracts;

public record CreateCategoryRequest(
    MultiLanguageField Name,
    IFormFile? Image
);
