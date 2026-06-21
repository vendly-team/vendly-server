using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.CategoryPrices;
using VendlyServer.Application.Services.CategoryPrices.Contracts;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class CategoryPriceServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CategoryPriceService _service;

    public CategoryPriceServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CategoryPriceService(_db);

        _db.Categories.Add(new Category { Id = 1, Name = "Cat1" });
        _db.Categories.Add(new Category { Id = 2, Name = "Cat2" });
        _db.SaveChanges();
    }

    private CreateCategoryPriceRequest CreateRequest(long categoryId = 1) =>
        new(categoryId, PriceMarkupType.Percent, 15m, 1_000m, null, null);

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAll_WhenNoFilter()
    {
        SeedPrice(id: 1, categoryId: 1);
        SeedPrice(id: 2, categoryId: 2);
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(categoryId: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByCategoryId()
    {
        SeedPrice(id: 1, categoryId: 1);
        SeedPrice(id: 2, categoryId: 2);
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(categoryId: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Single().CategoryId);
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeleted()
    {
        SeedPrice(id: 1, categoryId: 1, isDeleted: true);
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(categoryId: null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsPrice_WhenExists()
    {
        SeedPrice(id: 5, categoryId: 1);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenSoftDeleted()
    {
        SeedPrice(id: 6, categoryId: 1, isDeleted: true);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(6);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.NotFound, result.Error);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_CreatesPrice_AndReturnsId()
    {
        var result = await _service.AddAsync(CreateRequest(categoryId: 1));

        Assert.True(result.IsSuccess);
        Assert.True(result.Data > 0);
        Assert.Equal(1, _db.CategoryPrices.Count());
    }

    [Fact]
    public async Task Add_ReturnsCategoryNotFound_WhenCategoryMissing()
    {
        var result = await _service.AddAsync(CreateRequest(categoryId: 999));

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.CategoryNotFound, result.Error);
        Assert.Empty(_db.CategoryPrices);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ModifiesFields_WhenExists()
    {
        SeedPrice(id: 10, categoryId: 1);
        await _db.SaveChangesAsync();

        var req = new UpdateCategoryPriceRequest(2, PriceMarkupType.Fixed, 7_000m, 500m, null, null);
        var result = await _service.UpdateAsync(10, req);

        Assert.True(result.IsSuccess);
        var updated = _db.CategoryPrices.Single(p => p.Id == 10);
        Assert.Equal(2, updated.CategoryId);
        Assert.Equal(PriceMarkupType.Fixed, updated.MarkupType);
        Assert.Equal(7_000m, updated.Value);
        Assert.Equal(500m, updated.RoundingStep);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var req = new UpdateCategoryPriceRequest(1, PriceMarkupType.Percent, 10m, null, null, null);
        var result = await _service.UpdateAsync(999, req);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_ReturnsCategoryNotFound_WhenTargetCategoryMissing()
    {
        SeedPrice(id: 11, categoryId: 1);
        await _db.SaveChangesAsync();

        var req = new UpdateCategoryPriceRequest(999, PriceMarkupType.Percent, 10m, null, null, null);
        var result = await _service.UpdateAsync(11, req);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.CategoryNotFound, result.Error);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenExists()
    {
        SeedPrice(id: 20, categoryId: 1);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(20);

        Assert.True(result.IsSuccess);
        Assert.True(_db.CategoryPrices.Single(p => p.Id == 20).IsDeleted);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryPriceErrors.NotFound, result.Error);
    }

    private void SeedPrice(long id, long categoryId, bool isDeleted = false)
    {
        _db.CategoryPrices.Add(new CategoryPrice
        {
            Id = id,
            CategoryId = categoryId,
            MarkupType = PriceMarkupType.Percent,
            Value = 10m,
            RoundingStep = null,
            IsDeleted = isDeleted
        });
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
