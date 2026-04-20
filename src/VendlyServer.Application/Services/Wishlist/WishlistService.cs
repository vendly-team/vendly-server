using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Wishlist.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;
using WishlistEntity = VendlyServer.Domain.Entities.Catalog.Wishlist;

namespace VendlyServer.Application.Services.Wishlist;

public class WishlistService(AppDbContext dbContext) : IWishlistService
{
    public async Task<Result<List<WishlistResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new WishlistResponse(w.Id, w.ProductId, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<WishlistResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var wishlist = await dbContext.Wishlists
            .AsNoTracking()
            .Where(w => w.Id == id && w.UserId == userId)
            .Select(w => new WishlistResponse(w.Id, w.ProductId, w.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return wishlist is null ? WishlistErrors.NotFound : wishlist;
    }

    public async Task<Result> AddAsync(long userId, AddWishlistRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Wishlists
            .AsNoTracking()
            .AnyAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken);

        if (exists) return WishlistErrors.AlreadyExists;

        dbContext.Wishlists.Add(new WishlistEntity
        {
            UserId = userId,
            ProductId = request.ProductId,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var wishlist = await dbContext.Wishlists
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, cancellationToken);

        if (wishlist is null) return WishlistErrors.NotFound;

        dbContext.Wishlists.Remove(wishlist);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
