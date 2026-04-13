using FluentValidation;

namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsCityRequestValidator : AbstractValidator<SaveBtsCityRequest>
{
    public SaveBtsCityRequestValidator()
    {
        RuleFor(x => x.RegionCode).NotEmpty().WithMessage("Region code is required.").MaximumLength(10).WithMessage("Region code must not exceed 10 characters.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(10).WithMessage("Code must not exceed 10 characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
    }
}
