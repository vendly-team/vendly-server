using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Carts;
using VendlyServer.Application.Services.Carts.Contracts;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class CartServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CartService _service;

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CartService(_db);

        var category = new Category { Id = 1, Name = "Cat" };
        var product = new Product { Id = 1, Name = "Prod", CategoryId = 1 };
        var variant = new ProductVariant
        {
            Id = 1, ProductId = 1, Quantity = 10, IsActive = true,
            Price = 50m, Name = "Var1"
        };

        _db.Categories.Add(category);
        _db.Products.Add(product);
        _db.ProductVariants.Add(variant);
        _db.SaveChanges();
    }

    // ── GetOrCreateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreate_CreatesCart_WhenUserHasNone()
    {
        var result = await _service.GetOrCreateAsync(userId: 99);

        Assert.True(result.IsSuccess);
        Assert.Equal(99, _db.Carts.Single().UserId);
    }

    [Fact]
    public async Task GetOrCreate_ReturnsExisting_WhenCartExists()
    {
        _db.Carts.Add(new Cart { Id = 1, UserId = 5 });
        await _db.SaveChangesAsync();

        var r1 = await _service.GetOrCreateAsync(userId: 5);
        var r2 = await _service.GetOrCreateAsync(userId: 5);

        Assert.True(r1.IsSuccess);
        Assert.Equal(r1.Data!.Id, r2.Data!.Id);
        Assert.Equal(1, _db.Carts.Count(c => c.UserId == 5));
    }

    // ── AddItemAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddItem_CreatesCartAndAddsItem_WhenNoCart()
    {
        var result = await _service.AddItemAsync(userId: 10, new CartItemRequest(1, 2));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal(2, result.Data.Items[0].Qty);
    }

    [Fact]
    public async Task AddItem_IncrementsQty_WhenVariantAlreadyInCart()
    {
        await _service.AddItemAsync(userId: 10, new CartItemRequest(1, 3));
        var result = await _service.AddItemAsync(userId: 10, new CartItemRequest(1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.Items.Single().Qty);
    }

    [Fact]
    public async Task AddItem_ReturnsVariantNotFound_WhenVariantMissing()
    {
        var result = await _service.AddItemAsync(userId: 10, new CartItemRequest(999, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CartErrors.VariantNotFound, result.Error);
    }

    [Fact]
    public async Task AddItem_ReturnsInsufficientStock_WhenQtyExceedsVariantQuantity()
    {
        var result = await _service.AddItemAsync(userId: 10, new CartItemRequest(1, 15));

        Assert.False(result.IsSuccess);
        Assert.Equal(CartErrors.InsufficientStock, result.Error);
    }

    // ── UpdateItemAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItem_SoftDeletesItem_WhenQtyIsZero()
    {
        var cart = await AddCartWithItem(userId: 20, variantId: 1, qty: 3, cartItemId: 100);

        var result = await _service.UpdateItemAsync(userId: 20, cartItemId: 100, new UpdateCartItemRequest(0));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!.Items);
    }

    [Fact]
    public async Task UpdateItem_UpdatesQty_WhenQtyIsPositive()
    {
        await AddCartWithItem(userId: 21, variantId: 1, qty: 2, cartItemId: 101);

        var result = await _service.UpdateItemAsync(userId: 21, cartItemId: 101, new UpdateCartItemRequest(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.Items.Single().Qty);
    }

    [Fact]
    public async Task UpdateItem_ReturnsItemNotFound_WhenCartMissing()
    {
        var result = await _service.UpdateItemAsync(userId: 999, cartItemId: 1, new UpdateCartItemRequest(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CartErrors.ItemNotFound, result.Error);
    }

    // ── RemoveItemAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveItem_SoftDeletesItem()
    {
        await AddCartWithItem(userId: 30, variantId: 1, qty: 1, cartItemId: 200);

        var result = await _service.RemoveItemAsync(userId: 30, cartItemId: 200);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!.Items);
        Assert.True(_db.CartItems.Single(i => i.Id == 200).IsDeleted);
    }

    [Fact]
    public async Task RemoveItem_ReturnsItemNotFound_WhenItemMissing()
    {
        _db.Carts.Add(new Cart { Id = 50, UserId = 40 });
        await _db.SaveChangesAsync();

        var result = await _service.RemoveItemAsync(userId: 40, cartItemId: 999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CartErrors.ItemNotFound, result.Error);
    }

    // ── ClearAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_SoftDeletesAllItems()
    {
        await AddCartWithItem(userId: 50, variantId: 1, qty: 2, cartItemId: 300);

        var result = await _service.ClearAsync(userId: 50);

        Assert.True(result.IsSuccess);
        Assert.All(_db.CartItems.Where(i => i.Cart.UserId == 50), i => Assert.True(i.IsDeleted));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Cart> AddCartWithItem(long userId, long variantId, int qty, long cartItemId)
    {
        var cart = new Cart { UserId = userId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        _db.CartItems.Add(new CartItem
        {
            Id = cartItemId,
            CartId = cart.Id,
            ProductVariantId = variantId,
            Qty = qty
        });
        await _db.SaveChangesAsync();

        return cart;
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
