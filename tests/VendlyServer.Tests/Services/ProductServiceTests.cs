using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Application.Services.Storage;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ProductService(
            _db,
            new StubStorageService(),
            NullLogger<ProductService>.Instance,
            Options.Create(new ClientOptions { BaseUrl = "https://client.example.com" }));

        // Seed category
        _db.Categories.AddRange(
            new Category { Id = 1, Name = "Electronics", IsActive = true },
            new Category { Id = 2, Name = "Clothing",    IsActive = true }
        );

        // Seed products
        _db.Products.AddRange(
            new Product { Id = 1, CategoryId = 1, Name = "Phone",   IsActive = true,  SyncSource = SyncSource.Manual  },
            new Product { Id = 2, CategoryId = 1, Name = "Tablet",  IsActive = false, SyncSource = SyncSource.External },
            new Product { Id = 3, CategoryId = 2, Name = "Deleted", IsActive = true,  SyncSource = SyncSource.Manual, IsDeleted = true }
        );

        // Seed variant types for product 1
        _db.VariantTypes.AddRange(
            new VariantType { Id = 1, ProductId = 1, Name = "Color",     DisplayOrder = 1 },
            new VariantType { Id = 2, ProductId = 1, Name = "Size",      DisplayOrder = 2 },
            new VariantType { Id = 3, ProductId = 2, Name = "Deleted VT", IsDeleted = true }
        );

        // Seed options for type 1 (Color) and type 2 (Size)
        _db.VariantOptions.AddRange(
            new VariantOption { Id = 1, VariantTypeId = 1, Name = "Red",  DisplayOrder = 1 },
            new VariantOption { Id = 2, VariantTypeId = 1, Name = "Blue", DisplayOrder = 2 },
            new VariantOption { Id = 3, VariantTypeId = 2, Name = "S",    DisplayOrder = 1 },
            new VariantOption { Id = 4, VariantTypeId = 2, Name = "M",    DisplayOrder = 2 }
        );

        // Seed a variant for product 1
        _db.ProductVariants.Add(
            new ProductVariant { Id = 1, ProductId = 1, Name = "Red / S", Price = 99.99m, Quantity = 10, IsActive = true, Images = new List<string> { "phone-red.jpg" } }
        );

        _db.ProductVariants.Add(
            new ProductVariant { Id = 2, ProductId = 2, Name = "Tablet / 64GB", Price = 149.99m, Quantity = 0, IsActive = true, Images = new List<string> { "tablet.jpg" } }
        );

        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeletedProducts()
    {
        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.DoesNotContain(result.Data, p => p.Name == "Deleted");
    }

    [Fact]
    public async Task GetAll_IncludesCategoryName()
    {
        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.All(result.Data!, p => Assert.NotEmpty(p.CategoryName));
    }

    [Fact]
    public async Task Search_ReturnsMatchedProducts_WithStorefrontFields()
    {
        var result = await _service.SearchAsync("pho");

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!);
        Assert.Equal("Phone", item.Name);
        Assert.Equal(99.99m, item.Price);
        Assert.Equal(1, item.SkuCount);
        Assert.True(item.IsAvailableForSale);
        Assert.Contains("phone-red.jpg", item.Images);
        Assert.Equal("https://client.example.com/product/phone-1", item.RedirectUrl);
    }

    [Fact]
    public async Task Search_ReturnsEmpty_WhenQueryIsTooShort()
    {
        var result = await _service.SearchAsync("p");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsProduct_WhenFound()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Phone", result.Data!.Name);
        Assert.Equal("Electronics", result.Data.CategoryName);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenDeleted()
    {
        var result = await _service.GetByIdAsync(3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsNewProductId()
    {
        var result = await _service.CreateAsync(new CreateProductRequest(1, "Headphones", null));

        Assert.True(result.IsSuccess);
        Assert.True(result.Data > 0);
        var saved = await _db.Products.FindAsync(result.Data);
        Assert.NotNull(saved);
        Assert.Equal("Headphones", saved.Name);
        Assert.True(saved.IsActive);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_UpdatesProduct_WhenFound()
    {
        var result = await _service.UpdateAsync(1, new UpdateProductRequest(2, "Phone Updated", "desc", false, SyncSource.Manual));

        Assert.True(result.IsSuccess);
        var updated = await _db.Products.FindAsync(1L);
        Assert.Equal("Phone Updated", updated!.Name);
        Assert.Equal(2, updated.CategoryId);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.UpdateAsync(999, new UpdateProductRequest(1, "X", null, true, SyncSource.Manual));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenFound()
    {
        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.Products.FindAsync(1L);
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_FlipsFromTrueToFalse()
    {
        var result = await _service.ToggleActiveAsync(1); // product 1 is active

        Assert.True(result.IsSuccess);
        var row = await _db.Products.FindAsync(1L);
        Assert.False(row!.IsActive);
    }

    [Fact]
    public async Task ToggleActive_FlipsFromFalseToTrue()
    {
        var result = await _service.ToggleActiveAsync(2); // product 2 is inactive

        Assert.True(result.IsSuccess);
        var row = await _db.Products.FindAsync(2L);
        Assert.True(row!.IsActive);
    }

    [Fact]
    public async Task ToggleActive_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.ToggleActiveAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    // ── AddVariantTypeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddVariantType_AddsType_WhenProductExists()
    {
        var result = await _service.AddVariantTypeAsync(1, new CreateVariantTypeRequest("Material"));

        Assert.True(result.IsSuccess);
        var saved = await _db.VariantTypes.SingleOrDefaultAsync(vt => vt.Name == "Material" && vt.ProductId == 1);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AddVariantType_ReturnsNotFound_WhenProductMissing()
    {
        var result = await _service.AddVariantTypeAsync(999, new CreateVariantTypeRequest("X"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    // ── DeleteVariantTypeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantType_SoftDeletes_WhenFound()
    {
        var result = await _service.DeleteVariantTypeAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.VariantTypes.FindAsync(1L);
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task DeleteVariantType_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteVariantTypeAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantTypeNotFound, result.Error);
    }

    // ── AddVariantOptionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AddVariantOption_AddsOption_WhenTypeExists()
    {
        var result = await _service.AddVariantOptionAsync(1, new CreateVariantOptionRequest("Green", null));

        Assert.True(result.IsSuccess);
        var saved = await _db.VariantOptions.SingleOrDefaultAsync(o => o.Name == "Green" && o.VariantTypeId == 1);
        Assert.NotNull(saved);
        Assert.Null(saved.ImageUrl);
    }

    [Fact]
    public async Task AddVariantOption_ReturnsVariantTypeNotFound_WhenTypeMissing()
    {
        var result = await _service.AddVariantOptionAsync(999, new CreateVariantOptionRequest("X", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantTypeNotFound, result.Error);
    }

    // ── DeleteVariantOptionAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantOption_SoftDeletes_WhenFound()
    {
        var result = await _service.DeleteVariantOptionAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.VariantOptions.FindAsync(1L);
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task DeleteVariantOption_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteVariantOptionAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.OptionNotFound, result.Error);
    }

    // ── RegenerateVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RegenerateVariants_CreatesAllCombinations()
    {
        // Product 1 has Color(Red,Blue) x Size(S,M) = 4 combinations
        var result = await _service.RegenerateVariantsAsync(1);

        Assert.True(result.IsSuccess);
        var variants = await _db.ProductVariants
            .Where(v => v.ProductId == 1 && !v.IsDeleted)
            .ToListAsync();

        // Existing "Red / S" (id=1) + 3 new = 4 total
        Assert.Equal(4, variants.Count);
    }

    [Fact]
    public async Task RegenerateVariants_ReturnsNotFound_WhenProductMissing()
    {
        var result = await _service.RegenerateVariantsAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task RegenerateVariants_ReturnsError_WhenTypeHasNoOptions()
    {
        // Add a variant type with no options to product 1
        _db.VariantTypes.Add(new VariantType { Id = 10, ProductId = 1, Name = "Empty Type" });
        await _db.SaveChangesAsync();

        var result = await _service.RegenerateVariantsAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantTypeHasNoOptions, result.Error);
    }

    // ── UpdateVariantAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateVariant_UpdatesFields_WhenFound()
    {
        var result = await _service.UpdateVariantAsync(1, new UpdateVariantRequest("Red / S v2", 149.99m, 5, false, null));

        Assert.True(result.IsSuccess);
        var row = await _db.ProductVariants.FindAsync(1L);
        Assert.Equal("Red / S v2", row!.Name);
        Assert.Equal(149.99m, row.Price);
        Assert.Equal(5, row.Quantity);
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task UpdateVariant_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.UpdateVariantAsync(999, new UpdateVariantRequest(null, 0, 0, true, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantNotFound, result.Error);
    }

    // ── DeleteVariantAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariant_SoftDeletes_WhenFound()
    {
        var result = await _service.DeleteVariantAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.ProductVariants.FindAsync(1L);
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task DeleteVariant_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteVariantAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantNotFound, result.Error);
    }

    // ── BulkUpdateVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateVariants_UpdatesAllVariants_WhenAllBelongToProduct()
    {
        // Seed a second variant for product 1
        _db.ProductVariants.Add(new ProductVariant { Id = 2, ProductId = 1, Name = "Blue / M", Price = 50m, Quantity = 5, IsActive = true, Images = new List<string>() });
        await _db.SaveChangesAsync();

        var request = new BulkUpdateVariantsRequest(
        [
            new BulkUpdateVariantItem(1, "Red / S v2",  199.99m, 20, false, null),
            new BulkUpdateVariantItem(2, "Blue / M v2", 299.99m, 10, true,  null)
        ]);

        var result = await _service.BulkUpdateVariantsAsync(1, request);

        Assert.True(result.IsSuccess);

        var v1 = await _db.ProductVariants.FindAsync(1L);
        Assert.Equal("Red / S v2", v1!.Name);
        Assert.Equal(199.99m, v1.Price);
        Assert.Equal(20, v1.Quantity);
        Assert.False(v1.IsActive);

        var v2 = await _db.ProductVariants.FindAsync(2L);
        Assert.Equal("Blue / M v2", v2!.Name);
        Assert.Equal(299.99m, v2.Price);
    }

    [Fact]
    public async Task BulkUpdateVariants_ReturnsNotFound_WhenProductMissing()
    {
        var request = new BulkUpdateVariantsRequest([new BulkUpdateVariantItem(1, null, 0, 0, true, null)]);

        var result = await _service.BulkUpdateVariantsAsync(999, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task BulkUpdateVariants_ReturnsVariantNotFound_WhenVariantMissing()
    {
        var request = new BulkUpdateVariantsRequest([new BulkUpdateVariantItem(999, null, 0, 0, true, null)]);

        var result = await _service.BulkUpdateVariantsAsync(1, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantNotFound, result.Error);
    }

    [Fact]
    public async Task BulkUpdateVariants_ReturnsVariantNotFound_WhenVariantBelongsToDifferentProduct()
    {
        // Seed a variant for product 2
        _db.ProductVariants.Add(new ProductVariant { Id = 50, ProductId = 2, Name = "Other", Price = 0, Quantity = 0, IsActive = true, Images = new List<string>() });
        await _db.SaveChangesAsync();

        // Try to bulk update product 1 but include a variant from product 2
        var request = new BulkUpdateVariantsRequest([new BulkUpdateVariantItem(50, null, 0, 0, true, null)]);

        var result = await _service.BulkUpdateVariantsAsync(1, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrors.VariantNotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private class StubStorageService : IStorageService
    {
        public Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
            => Task.FromResult(Result<string>.Success($"/{folder}/{file.FileName}"));

        public Task<Result> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }
}
