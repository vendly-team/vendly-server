using FluentValidation;

namespace VendlyServer.Application.Services.Products.Contracts;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Name).NotNull();
        RuleFor(x => x.Name.Uz).MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Name.Ru).MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Name).Must(n => n is not null && (n.Uz != null || n.Ru != null))
            .WithMessage("At least one language name is required.");
    }
}
