using FluentValidation;

namespace VendlyServer.Application.Services.Checkout.Contracts;

public class InitiatePaymentRequestValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentRequestValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
    }
}
