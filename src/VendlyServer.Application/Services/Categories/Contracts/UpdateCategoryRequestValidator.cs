using FluentValidation;

namespace VendlyServer.Application.Services.Categories.Contracts;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotNull().WithMessage("Name is required.");
        RuleFor(x => x.Name.Uz).MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Name.Ru).MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Name).Must(n => n is not null && (n.Uz != null || n.Ru != null))
            .WithMessage("At least one language name is required.");
    }
}
