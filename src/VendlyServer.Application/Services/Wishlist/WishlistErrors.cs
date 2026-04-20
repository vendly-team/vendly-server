using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Wishlist;

public static class WishlistErrors
{
    public static readonly Error NotFound = Error.NotFound("Wishlist.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("Wishlist.AlreadyExists");
}
