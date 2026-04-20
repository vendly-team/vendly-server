using FluentValidation;

namespace VendlyServer.Application.Services.Wishlist.Contracts;

public class AddWishlistRequestValidator : AbstractValidator<AddWishlistRequest>
{
    public AddWishlistRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0.");
    }
}
