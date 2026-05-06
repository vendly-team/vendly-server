using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Categories;

public static class CategoryErrors
{
    public static readonly Error NotFound = Error.NotFound("Category.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("Category.AlreadyExists");
}
