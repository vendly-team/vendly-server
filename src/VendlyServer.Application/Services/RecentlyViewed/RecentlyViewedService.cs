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
            .Where(r => r.UserId == userId && !r.IsDeleted)
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
            .SingleOrDefaultAsync(r => r.UserId == userId && r.ProductId == request.ProductId && !r.IsDeleted, cancellationToken);

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

        var existing = await dbContext.RecentlyViewedProducts
            .Where(r => r.UserId == userId && validProductIds.Contains(r.ProductId) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var existingMap = existing.ToDictionary(r => r.ProductId);

        // Order received from client preserves user's view order (oldest first → newest last).
        // Stagger ViewedAt by milliseconds so ordering is preserved server-side.
        for (var i = 0; i < validProductIds.Count; i++)
        {
            var productId = validProductIds[i];
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
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0) return Result.Success();

        foreach (var entry in entries)
            entry.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task TrimToMaxAsync(long userId, CancellationToken cancellationToken)
    {
        var overflow = await dbContext.RecentlyViewedProducts
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.ViewedAt)
            .Skip(MaxItemsPerUser)
            .ToListAsync(cancellationToken);

        if (overflow.Count == 0) return;

        foreach (var entry in overflow)
            entry.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
