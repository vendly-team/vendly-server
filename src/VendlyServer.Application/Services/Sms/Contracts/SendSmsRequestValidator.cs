using FluentValidation;

namespace VendlyServer.Application.Services.Sms.Contracts;

public class SendSmsRequestValidator : AbstractValidator<SendSmsRequest>
{
    public SendSmsRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^998\d{9}$").WithMessage("Phone must be in 998XXXXXXXXX format.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(918).WithMessage("Message must not exceed 918 characters.");
    }
}
