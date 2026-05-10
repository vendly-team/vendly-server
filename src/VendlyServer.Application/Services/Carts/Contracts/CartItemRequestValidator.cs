using FluentValidation;

namespace VendlyServer.Application.Services.Carts.Contracts;

public class CartItemRequestValidator : AbstractValidator<CartItemRequest>
{
    public CartItemRequestValidator()
    {
        RuleFor(x => x.ProductVariantId).GreaterThan(0);
        RuleFor(x => x.Qty).GreaterThan(0);
    }
}
