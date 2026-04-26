using FluentValidation;

namespace VendlyServer.Application.Services.Products.Contracts;

public class CreateVariantOptionRequestValidator : AbstractValidator<CreateVariantOptionRequest>
{
    public CreateVariantOptionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
