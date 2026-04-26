using FluentValidation;

namespace VendlyServer.Application.Services.Products.Contracts;

public class BulkUpdateVariantsRequestValidator : AbstractValidator<BulkUpdateVariantsRequest>
{
    public BulkUpdateVariantsRequestValidator()
    {
        RuleFor(x => x.Variants).NotEmpty();

        RuleForEach(x => x.Variants).ChildRules(item =>
        {
            item.RuleFor(v => v.Id).GreaterThan(0);
            item.RuleFor(v => v.Price).GreaterThanOrEqualTo(0);
            item.RuleFor(v => v.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}
