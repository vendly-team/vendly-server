using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.RecentlyViewed;
using VendlyServer.Application.Services.RecentlyViewed.Contracts;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class RecentlyViewedServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecentlyViewedService _service;
    private readonly DateTime _baseTime = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    public RecentlyViewedServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new RecentlyViewedService(_db);

        // Products available for tracking. Product 99 is soft-deleted.
        _db.Products.AddRange(
            new Product { Id = 10, CategoryId = 1, Name = "P10" },
            new Product { Id = 20, CategoryId = 1, Name = "P20" },
            new Product { Id = 30, CategoryId = 1, Name = "P30" },
            new Product { Id = 99, CategoryId = 1, Name = "P99-deleted", IsDeleted = true }
        );

        // user 1 has 2 entries; user 2 has 1 entry on the same product 10.
        _db.RecentlyViewedProducts.AddRange(
            new RecentlyViewedProduct { Id = 1, UserId = 1, ProductId = 10, ViewedAt = _baseTime.AddMinutes(-10), CreatedAt = _baseTime.AddMinutes(-10) },
            new RecentlyViewedProduct { Id = 2, UserId = 1, ProductId = 20, ViewedAt = _baseTime.AddMinutes(-5),  CreatedAt = _baseTime.AddMinutes(-5)  },
            new RecentlyViewedProduct { Id = 3, UserId = 2, ProductId = 10, ViewedAt = _baseTime.AddMinutes(-1),  CreatedAt = _baseTime.AddMinutes(-1)  }
        );
        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersItems_OrderedByViewedAtDescending()
    {
        var result = await _service.GetAllAsync(userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(20, result.Data[0].ProductId); // newest first
        Assert.Equal(10, result.Data[1].ProductId);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenUserHasNoItems()
    {
        var result = await _service.GetAllAsync(userId: 999);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAll_ReturnsAtMost20Items()
    {
        // Seed 25 entries for user 3.
        for (var i = 0; i < 25; i++)
        {
            _db.Products.Add(new Product { Id = 1000 + i, CategoryId = 1, Name = $"Bulk{i}" });
            _db.RecentlyViewedProducts.Add(new RecentlyViewedProduct
            {
                UserId = 3,
                ProductId = 1000 + i,
                ViewedAt = _baseTime.AddMinutes(i),
                CreatedAt = _baseTime.AddMinutes(i),
            });
        }
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(userId: 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Data!.Count);
    }

    // ── TrackAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Track_CreatesNewEntry_WhenProductNotYetTracked()
    {
        var result = await _service.TrackAsync(userId: 1, new TrackProductViewRequest(ProductId: 30));

        Assert.True(result.IsSuccess);
        var saved = await _db.RecentlyViewedProducts.SingleOrDefaultAsync(r => r.UserId == 1 && r.ProductId == 30);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Track_UpdatesViewedAt_WhenProductAlreadyTracked()
    {
        var beforeTrack = await _db.RecentlyViewedProducts.AsNoTracking().SingleAsync(r => r.UserId == 1 && r.ProductId == 10);

        var result = await _service.TrackAsync(userId: 1, new TrackProductViewRequest(ProductId: 10));

        Assert.True(result.IsSuccess);
        var afterTrack = await _db.RecentlyViewedProducts.AsNoTracking().SingleAsync(r => r.UserId == 1 && r.ProductId == 10);
        Assert.True(afterTrack.ViewedAt > beforeTrack.ViewedAt);
        Assert.Equal(beforeTrack.Id, afterTrack.Id); // same row, no duplicate
    }

    [Fact]
    public async Task Track_ReturnsProductNotFound_WhenProductDoesNotExist()
    {
        var result = await _service.TrackAsync(userId: 1, new TrackProductViewRequest(ProductId: 88888));

        Assert.False(result.IsSuccess);
        Assert.Equal(RecentlyViewedErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Track_ReturnsProductNotFound_WhenProductSoftDeleted()
    {
        var result = await _service.TrackAsync(userId: 1, new TrackProductViewRequest(ProductId: 99));

        Assert.False(result.IsSuccess);
        Assert.Equal(RecentlyViewedErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Track_TrimsOldestEntry_WhenExceedingMaxOf20()
    {
        // Seed 20 entries for user 4 (the limit), plus 20 new products to track.
        for (var i = 0; i < 20; i++)
        {
            _db.Products.Add(new Product { Id = 2000 + i, CategoryId = 1, Name = $"Trim{i}" });
            _db.RecentlyViewedProducts.Add(new RecentlyViewedProduct
            {
                UserId = 4,
                ProductId = 2000 + i,
                ViewedAt = _baseTime.AddMinutes(i), // oldest is product 2000
                CreatedAt = _baseTime.AddMinutes(i),
            });
        }
        _db.Products.Add(new Product { Id = 3000, CategoryId = 1, Name = "Newest" });
        await _db.SaveChangesAsync();

        var result = await _service.TrackAsync(userId: 4, new TrackProductViewRequest(ProductId: 3000));

        Assert.True(result.IsSuccess);
        var entries = await _db.RecentlyViewedProducts.Where(r => r.UserId == 4).ToListAsync();
        Assert.Equal(20, entries.Count);
        Assert.DoesNotContain(entries, r => r.ProductId == 2000); // oldest dropped
        Assert.Contains(entries, r => r.ProductId == 3000); // newest kept
    }

    // ── BulkSyncAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkSync_ReturnsSuccess_WhenInputEmpty()
    {
        var result = await _service.BulkSyncAsync(userId: 1, new BulkSyncRecentlyViewedRequest(ProductIds: []));

        Assert.True(result.IsSuccess);
        var entries = await _db.RecentlyViewedProducts.Where(r => r.UserId == 1).ToListAsync();
        Assert.Equal(2, entries.Count); // unchanged
    }

    [Fact]
    public async Task BulkSync_AddsNewEntries_AndUpdatesExisting()
    {
        // user 1 has products 10, 20. Sync [10, 30] — 10 should update, 30 should be added.
        var existingViewedAt = (await _db.RecentlyViewedProducts.AsNoTracking().SingleAsync(r => r.UserId == 1 && r.ProductId == 10)).ViewedAt;

        var result = await _service.BulkSyncAsync(userId: 1, new BulkSyncRecentlyViewedRequest(ProductIds: new[] { 10L, 30L }));

        Assert.True(result.IsSuccess);
        var entries = await _db.RecentlyViewedProducts.AsNoTracking().Where(r => r.UserId == 1).ToListAsync();
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, r => r.ProductId == 30);
        Assert.True(entries.Single(r => r.ProductId == 10).ViewedAt > existingViewedAt);
    }

    [Fact]
    public async Task BulkSync_FiltersOutInvalidProductIds()
    {
        // Product 88888 doesn't exist; product 99 is soft-deleted; product 30 is valid.
        var result = await _service.BulkSyncAsync(userId: 1, new BulkSyncRecentlyViewedRequest(ProductIds: new[] { 88888L, 99L, 30L }));

        Assert.True(result.IsSuccess);
        var newEntries = await _db.RecentlyViewedProducts.AsNoTracking().Where(r => r.UserId == 1).Select(r => r.ProductId).ToListAsync();
        Assert.Contains(30L, newEntries);
        Assert.DoesNotContain(88888L, newEntries);
        Assert.DoesNotContain(99L, newEntries);
    }

    [Fact]
    public async Task BulkSync_TrimsToMaxOf20()
    {
        // Seed 15 entries for user 5, sync 10 more new products → total 25, trim to 20.
        for (var i = 0; i < 15; i++)
        {
            _db.Products.Add(new Product { Id = 4000 + i, CategoryId = 1, Name = $"Pre{i}" });
            _db.RecentlyViewedProducts.Add(new RecentlyViewedProduct
            {
                UserId = 5,
                ProductId = 4000 + i,
                ViewedAt = _baseTime.AddMinutes(i),
                CreatedAt = _baseTime.AddMinutes(i),
            });
        }
        var newIds = new List<long>();
        for (var i = 0; i < 10; i++)
        {
            _db.Products.Add(new Product { Id = 5000 + i, CategoryId = 1, Name = $"New{i}" });
            newIds.Add(5000 + i);
        }
        await _db.SaveChangesAsync();

        var result = await _service.BulkSyncAsync(userId: 5, new BulkSyncRecentlyViewedRequest(ProductIds: newIds));

        Assert.True(result.IsSuccess);
        var entries = await _db.RecentlyViewedProducts.Where(r => r.UserId == 5).ToListAsync();
        Assert.Equal(20, entries.Count);
    }

    // ── ClearAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_RemovesAllEntries_ForCurrentUser()
    {
        var result = await _service.ClearAsync(userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Empty(await _db.RecentlyViewedProducts.Where(r => r.UserId == 1).ToListAsync());
    }

    [Fact]
    public async Task Clear_DoesNotTouchOtherUsers()
    {
        await _service.ClearAsync(userId: 1);

        var otherUserEntries = await _db.RecentlyViewedProducts.Where(r => r.UserId == 2).ToListAsync();
        Assert.Single(otherUserEntries);
    }

    [Fact]
    public async Task Clear_ReturnsSuccess_WhenNothingToClear()
    {
        var result = await _service.ClearAsync(userId: 999);

        Assert.True(result.IsSuccess);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
