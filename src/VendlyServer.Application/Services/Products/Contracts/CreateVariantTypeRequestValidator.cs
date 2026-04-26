using FluentValidation;

namespace VendlyServer.Application.Services.Products.Contracts;

public class CreateVariantTypeRequestValidator : AbstractValidator<CreateVariantTypeRequest>
{
    public CreateVariantTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
