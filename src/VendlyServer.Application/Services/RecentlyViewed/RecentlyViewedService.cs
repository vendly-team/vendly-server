using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Persistence;
using VendlyServer.Application.Services.RecentlyViewed.Contracts;

namespace VendlyServer.Application.Services.RecentlyViewed;

public class RecentlyViewedService(AppDbContext dbContext) : IRecentlyViewedService
{
    private const int MaxItemsPerUser = 20;

    public async Task<Result<List<RecentlyViewedResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RecentlyViewedProducts
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ViewedAt)
            .Take(MaxItemsPerUser)
            .Select(r => new RecentlyViewedResponse(r.Id, r.ProductId, r.ViewedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> TrackAsync(long userId, TrackProductViewRequest request, CancellationToken cancellationToken = default)
    {
        var productExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

        if (!productExists) return RecentlyViewedErrors.ProductNotFound;

        var now = DateTime.UtcNow;

        var existing = await dbContext.RecentlyViewedProducts
            .SingleOrDefaultAsync(r => r.UserId == userId && r.ProductId == request.ProductId, cancellationToken);

        if (existing is not null)
        {
            existing.ViewedAt = now;
        }
        else
        {
            dbContext.RecentlyViewedProducts.Add(new RecentlyViewedProduct
            {
                UserId = userId,
                ProductId = request.ProductId,
                ViewedAt = now,
                CreatedAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await TrimToMaxAsync(userId, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> BulkSyncAsync(long userId, BulkSyncRecentlyViewedRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductIds.Count == 0) return Result.Success();

        var validProductIds = await dbContext.Products
            .AsNoTracking()
            .Where(p => request.ProductIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (validProductIds.Count == 0) return Result.Success();

        // Re-order validated IDs to match the client-supplied sequence so that staggered
        // ViewedAt timestamps reflect the client's intended "oldest first → newest last" order,
        // not the database's natural primary-key ordering returned by ToListAsync.
        var validSet = new HashSet<long>(validProductIds);
        var orderedValidIds = request.ProductIds
            .Where(id => validSet.Contains(id))
            .ToList();

        var existing = await dbContext.RecentlyViewedProducts
            .Where(r => r.UserId == userId && orderedValidIds.Contains(r.ProductId))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var existingMap = existing.ToDictionary(r => r.ProductId);

        // Order received from client preserves user's view order (oldest first → newest last).
        // Stagger ViewedAt by milliseconds so ordering is preserved server-side.
        for (var i = 0; i < orderedValidIds.Count; i++)
        {
            var productId = orderedValidIds[i];
            var viewedAt = now.AddMilliseconds(i);

            if (existingMap.TryGetValue(productId, out var entry))
            {
                if (entry.ViewedAt < viewedAt) entry.ViewedAt = viewedAt;
            }
            else
            {
                dbContext.RecentlyViewedProducts.Add(new RecentlyViewedProduct
                {
                    UserId = userId,
                    ProductId = productId,
                    ViewedAt = viewedAt,
                    CreatedAt = viewedAt
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await TrimToMaxAsync(userId, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ClearAsync(long userId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.RecentlyViewedProducts
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0) return Result.Success();

        dbContext.RecentlyViewedProducts.RemoveRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task TrimToMaxAsync(long userId, CancellationToken cancellationToken)
    {
        var overflow = await dbContext.RecentlyViewedProducts
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ViewedAt)
            .Skip(MaxItemsPerUser)
            .ToListAsync(cancellationToken);

        if (overflow.Count == 0) return;

        dbContext.RecentlyViewedProducts.RemoveRange(overflow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
