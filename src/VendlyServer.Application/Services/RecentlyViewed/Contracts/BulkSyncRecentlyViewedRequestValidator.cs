using FluentValidation;

namespace VendlyServer.Application.Services.RecentlyViewed.Contracts;

public class BulkSyncRecentlyViewedRequestValidator : AbstractValidator<BulkSyncRecentlyViewedRequest>
{
    public BulkSyncRecentlyViewedRequestValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotNull()
            .Must(ids => ids.Count <= 20).WithMessage("Cannot sync more than 20 product ids at once.");

        RuleForEach(x => x.ProductIds)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0.");
    }
}
