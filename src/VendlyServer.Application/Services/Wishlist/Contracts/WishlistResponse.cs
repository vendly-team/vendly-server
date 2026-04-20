namespace VendlyServer.Application.Services.Wishlist.Contracts;

public record WishlistResponse(
    long Id,
    long ProductId,
    DateTime CreatedAt
);
