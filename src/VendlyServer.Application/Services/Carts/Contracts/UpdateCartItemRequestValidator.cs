using FluentValidation;

namespace VendlyServer.Application.Services.Carts.Contracts;

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        RuleFor(x => x.Qty).GreaterThanOrEqualTo(0);
    }
}
