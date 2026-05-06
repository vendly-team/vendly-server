using FluentValidation;

namespace VendlyServer.Application.Services.RecentlyViewed.Contracts;

public class TrackProductViewRequestValidator : AbstractValidator<TrackProductViewRequest>
{
    public TrackProductViewRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0.");
    }
}
