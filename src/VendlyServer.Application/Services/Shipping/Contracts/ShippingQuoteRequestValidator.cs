using FluentValidation;

namespace VendlyServer.Application.Services.Shipping.Contracts;

public class ShippingQuoteRequestValidator : AbstractValidator<ShippingQuoteRequest>
{
    public ShippingQuoteRequestValidator()
    {
        RuleFor(x => x.ReceiverCityCode)
            .NotEmpty().WithMessage("ReceiverCityCode is required.")
            .MaximumLength(10).WithMessage("ReceiverCityCode must not exceed 10 characters.");

        RuleFor(x => x.ReceiverBranchCode)
            .MaximumLength(10).WithMessage("ReceiverBranchCode must not exceed 10 characters.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than zero.");
    }
}
