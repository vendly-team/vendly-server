using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Wishlist;
using VendlyServer.Application.Services.Wishlist.Contracts;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class WishlistServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WishlistService _service;

    public WishlistServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new WishlistService(_db);

        _db.Wishlists.AddRange(
            new Wishlist { Id = 1, UserId = 1, ProductId = 10, CreatedAt = DateTime.UtcNow },
            new Wishlist { Id = 2, UserId = 1, ProductId = 20, CreatedAt = DateTime.UtcNow },
            new Wishlist { Id = 3, UserId = 2, ProductId = 10, CreatedAt = DateTime.UtcNow }
        );
        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersItems()
    {
        var result = await _service.GetAllAsync(userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data, w => Assert.True(w.ProductId == 10 || w.ProductId == 20));
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenUserHasNoItems()
    {
        var result = await _service.GetAllAsync(userId: 999);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsItem_WhenIdAndUserMatch()
    {
        var result = await _service.GetByIdAsync(id: 1, userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.Id);
        Assert.Equal(10, result.Data.ProductId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdDoesNotExist()
    {
        var result = await _service.GetByIdAsync(id: 999, userId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(WishlistErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenItemBelongsToDifferentUser()
    {
        var result = await _service.GetByIdAsync(id: 1, userId: 2); // id=1 belongs to user 1

        Assert.False(result.IsSuccess);
        Assert.Equal(WishlistErrors.NotFound, result.Error);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_CreatesItem_WhenNotDuplicate()
    {
        var result = await _service.AddAsync(userId: 1, new AddWishlistRequest(ProductId: 99));

        Assert.True(result.IsSuccess);
        var saved = await _db.Wishlists.SingleOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 99);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Add_ReturnsAlreadyExists_WhenDuplicate()
    {
        var result = await _service.AddAsync(userId: 1, new AddWishlistRequest(ProductId: 10));

        Assert.False(result.IsSuccess);
        Assert.Equal(WishlistErrors.AlreadyExists, result.Error);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesItem_WhenFound()
    {
        var result = await _service.DeleteAsync(id: 1, userId: 1);

        Assert.True(result.IsSuccess);
        var gone = await _db.Wishlists.FindAsync(1L);
        Assert.Null(gone);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenIdDoesNotExist()
    {
        var result = await _service.DeleteAsync(id: 999, userId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(WishlistErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenItemBelongsToDifferentUser()
    {
        var result = await _service.DeleteAsync(id: 1, userId: 2); // id=1 belongs to user 1

        Assert.False(result.IsSuccess);
        Assert.Equal(WishlistErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
