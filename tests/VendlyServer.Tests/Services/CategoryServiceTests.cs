using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VendlyServer.Application.Services.Category;
using VendlyServer.Application.Services.Category.Contracts;
using VendlyServer.Application.Services.Storage;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CategoryService(_db, new StubStorageService(), NullLogger<CategoryService>.Instance);

        _db.Categories.AddRange(
            new Category { Id = 1, Name = "Electronics", IsActive = true },
            new Category { Id = 2, Name = "Clothing",    IsActive = false },
            new Category { Id = 3, Name = "Deleted",     IsActive = true, IsDeleted = true }
        );
        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeleted()
    {
        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.DoesNotContain(result.Data, c => c.Name == "Deleted");
    }

    [Fact]
    public async Task GetAll_ReturnsIsActive_Correctly()
    {
        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Data!, c => c.Name == "Electronics" && c.IsActive);
        Assert.Contains(result.Data!, c => c.Name == "Clothing" && !c.IsActive);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsCategory_WhenExists()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Electronics", result.Data!.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenDeleted()
    {
        var result = await _service.GetByIdAsync(3);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNotExists()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.NotFound, result.Error);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_InsertsCategory_WhenNameIsUnique()
    {
        var result = await _service.AddAsync(new CreateCategoryRequest("Furniture", null));

        Assert.True(result.IsSuccess);
        var saved = await _db.Categories.SingleOrDefaultAsync(c => c.Name == "Furniture");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Add_ReturnsAlreadyExists_WhenNameDuplicate()
    {
        var result = await _service.AddAsync(new CreateCategoryRequest("Electronics", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.AlreadyExists, result.Error);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_UpdatesName_WhenFound()
    {
        var result = await _service.UpdateAsync(1, new UpdateCategoryRequest("Updated", null));

        Assert.True(result.IsSuccess);
        var updated = await _db.Categories.FindAsync(1L);
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenNotExists()
    {
        var result = await _service.UpdateAsync(999, new UpdateCategoryRequest("X", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.NotFound, result.Error);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenFound()
    {
        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.Categories.FindAsync(1L);
        Assert.NotNull(row);
        Assert.True(row.IsDeleted);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNotExists()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.NotFound, result.Error);
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_FlipsToFalse_WhenCurrentlyActive()
    {
        var result = await _service.ToggleActiveAsync(1);

        Assert.True(result.IsSuccess);
        var row = await _db.Categories.FindAsync(1L);
        Assert.False(row!.IsActive);
    }

    [Fact]
    public async Task ToggleActive_FlipsToTrue_WhenCurrentlyInactive()
    {
        var result = await _service.ToggleActiveAsync(2);

        Assert.True(result.IsSuccess);
        var row = await _db.Categories.FindAsync(2L);
        Assert.True(row!.IsActive);
    }

    [Fact]
    public async Task ToggleActive_ReturnsNotFound_WhenNotExists()
    {
        var result = await _service.ToggleActiveAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private class StubStorageService : IStorageService
    {
        public Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
            => Task.FromResult(Result<string>.Success(file.FileName));

        public Task<Result> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }
}
