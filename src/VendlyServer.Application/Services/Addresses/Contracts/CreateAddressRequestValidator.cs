using FluentValidation;

namespace VendlyServer.Application.Services.Addresses.Contracts;

public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(50).WithMessage("Label must not exceed 50 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.")
            .MaximumLength(100).WithMessage("District must not exceed 100 characters.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required.")
            .MaximumLength(255).WithMessage("Street must not exceed 255 characters.");

        RuleFor(x => x.House)
            .NotEmpty().WithMessage("House is required.")
            .MaximumLength(50).WithMessage("House must not exceed 50 characters.");

        RuleFor(x => x.Extra)
            .MaximumLength(255).WithMessage("Extra must not exceed 255 characters.");

        RuleFor(x => x.BtsCityCode)
            .NotEmpty().WithMessage("BtsCityCode is required.")
            .MaximumLength(10).WithMessage("BtsCityCode must not exceed 10 characters.");

        RuleFor(x => x.BtsBranchCode)
            .MaximumLength(10).WithMessage("BtsBranchCode must not exceed 10 characters.");
    }
}
