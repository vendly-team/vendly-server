using FluentValidation;

namespace VendlyServer.Application.Services.Checkout.Contracts;

public class CreateCheckoutRequestValidator : AbstractValidator<CreateCheckoutRequest>
{
    public CreateCheckoutRequestValidator()
    {
        RuleFor(x => x.AddressId).GreaterThan(0);
    }
}
