using FluentValidation;

namespace VendlyServer.Application.Services.Products.Contracts;

public class ProductFilterRequestValidator : AbstractValidator<ProductFilterRequest>
{
    public ProductFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
