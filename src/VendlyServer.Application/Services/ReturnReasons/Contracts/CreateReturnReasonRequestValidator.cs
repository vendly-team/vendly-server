using FluentValidation;

namespace VendlyServer.Application.Services.ReturnReasons.Contracts;

public class CreateReturnReasonRequestValidator : AbstractValidator<CreateReturnReasonRequest>
{
    public CreateReturnReasonRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Key is required.")
            .MaximumLength(100).WithMessage("Key must not exceed 100 characters.")
            .Matches(@"^[A-Z][A-Z0-9_]*$").WithMessage("Key must be UPPERCASE_SNAKE_CASE.");

        RuleFor(x => x.Name)
            .NotNull().WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name.Uz) || !string.IsNullOrWhiteSpace(name.Ru))
            .WithMessage("At least Uz or Ru name must be provided.");
    }
}
