using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Products;

public static class ProductErrors
{
    public static readonly Error NotFound            = Error.NotFound("Product.NotFound");
    public static readonly Error VariantTypeNotFound = Error.NotFound("Product.VariantTypeNotFound");
    public static readonly Error OptionNotFound      = Error.NotFound("Product.OptionNotFound");
    public static readonly Error VariantNotFound     = Error.NotFound("Product.VariantNotFound");
    public static readonly Error DuplicateOptionName = Error.Conflict("Product.DuplicateOptionName");
    public static readonly Error VariantTypeHasNoOptions = Error.Failure("Product.VariantTypeHasNoOptions");
}
