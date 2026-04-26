using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Category;

public static class CategoryErrors
{
    public static readonly Error NotFound = Error.NotFound("Category.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("Category.AlreadyExists");
}
