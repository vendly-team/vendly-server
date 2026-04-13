using FluentValidation;

namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsBranchRequestValidator : AbstractValidator<SaveBtsBranchRequest>
{
    public SaveBtsBranchRequestValidator()
    {
        RuleFor(x => x.RegionCode).NotEmpty().WithMessage("Region code is required.").MaximumLength(10).WithMessage("Region code must not exceed 10 characters.");
        RuleFor(x => x.CityCode).NotEmpty().WithMessage("City code is required.").MaximumLength(10).WithMessage("City code must not exceed 10 characters.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(10).WithMessage("Code must not exceed 10 characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
        RuleFor(x => x.Phone).MaximumLength(20).WithMessage("Phone must not exceed 20 characters.").When(x => x.Phone != null);
        RuleFor(x => x.LatLong).MaximumLength(50).WithMessage("Lat/long must not exceed 50 characters.").When(x => x.LatLong != null);
    }
}
