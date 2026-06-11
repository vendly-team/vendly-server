using FluentValidation;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.CategoryPrices.Contracts;

public class UpdateCategoryPriceRequestValidator : AbstractValidator<UpdateCategoryPriceRequest>
{
    public UpdateCategoryPriceRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);

        RuleFor(x => x.MarkupType).IsInEnum();

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Value must be zero or greater.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(1000).When(x => x.MarkupType == PriceMarkupType.Percent)
            .WithMessage("Percent value must not exceed 1000.");

        RuleFor(x => x.RoundingStep)
            .GreaterThan(0).When(x => x.RoundingStep.HasValue)
            .WithMessage("RoundingStep must be greater than zero.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}
