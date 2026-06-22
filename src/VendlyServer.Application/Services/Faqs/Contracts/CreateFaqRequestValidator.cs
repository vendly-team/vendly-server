using FluentValidation;

namespace VendlyServer.Application.Services.Faqs.Contracts;

public class CreateFaqRequestValidator : AbstractValidator<CreateFaqRequest>
{
    public CreateFaqRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotNull().WithMessage("Question is required.")
            .Must(q => !string.IsNullOrWhiteSpace(q.Uz) || !string.IsNullOrWhiteSpace(q.Ru))
            .WithMessage("At least Uz or Ru question must be provided.");

        RuleFor(x => x.Answer)
            .NotNull().WithMessage("Answer is required.")
            .Must(a => !string.IsNullOrWhiteSpace(a.Uz) || !string.IsNullOrWhiteSpace(a.Ru))
            .WithMessage("At least Uz or Ru answer must be provided.");
    }
}
