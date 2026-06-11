using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Carts;

public static class CartErrors
{
    public static readonly Error ItemNotFound      = Error.NotFound("Cart.ItemNotFound");
    public static readonly Error VariantNotFound   = Error.NotFound("Cart.VariantNotFound");
    public static readonly Error InsufficientStock = Error.Failure("Cart.InsufficientStock");
    public static readonly Error InvalidQty          = Error.Failure("Cart.InvalidQty");
    public static readonly Error CheckoutInProgress  = Error.Conflict("Cart.CheckoutInProgress");
}
