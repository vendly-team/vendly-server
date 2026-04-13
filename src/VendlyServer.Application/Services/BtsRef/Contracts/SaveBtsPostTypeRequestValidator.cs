using FluentValidation;

namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsPostTypeRequestValidator : AbstractValidator<SaveBtsPostTypeRequest>
{
    public SaveBtsPostTypeRequestValidator()
    {
        RuleFor(x => x.BtsId).GreaterThan(0).WithMessage("BTS ID must be greater than 0.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
    }
}
