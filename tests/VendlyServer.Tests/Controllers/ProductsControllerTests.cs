using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly FakeProductService _svc = new();

    private ProductsController CreateController()
    {
        var ctrl = new ProductsController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200_WithProductList()
    {
        _svc.GetAllResult = Result<List<ProductListResponse>>.Success(
        [
            new(1, 1, "Electronics", "Phone",  null, SyncSource.Manual,   true,  2, DateTime.UtcNow, null),
            new(2, 1, "Electronics", "Tablet", null, SyncSource.External, false, 0, DateTime.UtcNow, null)
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<ProductListResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllResult = Result<List<ProductListResponse>>.Failure(ProductErrors.NotFound);

        var result = await CreateController().GetAllAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        _svc.GetByIdResult = Result<ProductAdminDetailResponse>.Success(
            new(1, 1, "Electronics", "Phone", null, SyncSource.Manual, true, [], [], DateTime.UtcNow, null));

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<ProductAdminDetailResponse>>(result);
        Assert.Equal("Phone", ok.Value!.Name);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_WhenNotFound()
    {
        _svc.GetByIdResult = Result<ProductAdminDetailResponse>.Failure(ProductErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns200_WithNewId()
    {
        _svc.CreateResult = Result<long>.Success(42);

        var result = await CreateController().CreateAsync(new CreateProductRequest(1, "Headphones", null));

        var ok = Assert.IsType<Ok<long>>(result);
        Assert.Equal(42L, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsProblem_OnFailure()
    {
        _svc.CreateResult = Result<long>.Failure(ProductErrors.NotFound);

        var result = await CreateController().CreateAsync(new CreateProductRequest(999, "X", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns200_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new UpdateProductRequest(1, "Phone v2", null, true, SyncSource.Manual));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_WhenNotFound()
    {
        _svc.UpdateResult = Result.Failure(ProductErrors.NotFound);

        var result = await CreateController().UpdateAsync(999, new UpdateProductRequest(1, "X", null, true, SyncSource.Manual));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns200_OnSuccess()
    {
        _svc.DeleteResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_WhenNotFound()
    {
        _svc.DeleteResult = Result.Failure(ProductErrors.NotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task Toggle_Returns200_OnSuccess()
    {
        _svc.ToggleResult = Result.Success();

        var result = await CreateController().ToggleActiveAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Toggle_ReturnsProblem_WhenNotFound()
    {
        _svc.ToggleResult = Result.Failure(ProductErrors.NotFound);

        var result = await CreateController().ToggleActiveAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── AddVariantTypeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddVariantType_Returns200_OnSuccess()
    {
        _svc.AddVariantTypeResult = Result.Success();

        var result = await CreateController().AddVariantTypeAsync(1, new CreateVariantTypeRequest("Color"));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task AddVariantType_ReturnsProblem_WhenProductNotFound()
    {
        _svc.AddVariantTypeResult = Result.Failure(ProductErrors.NotFound);

        var result = await CreateController().AddVariantTypeAsync(999, new CreateVariantTypeRequest("Color"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── DeleteVariantTypeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantType_Returns200_OnSuccess()
    {
        _svc.DeleteVariantTypeResult = Result.Success();

        var result = await CreateController().DeleteVariantTypeAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task DeleteVariantType_ReturnsProblem_WhenNotFound()
    {
        _svc.DeleteVariantTypeResult = Result.Failure(ProductErrors.VariantTypeNotFound);

        var result = await CreateController().DeleteVariantTypeAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── AddVariantOptionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AddVariantOption_Returns200_OnSuccess()
    {
        _svc.AddVariantOptionResult = Result.Success();

        var result = await CreateController().AddVariantOptionAsync(1, new CreateVariantOptionRequest("Red", null));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task AddVariantOption_ReturnsProblem_WhenTypeNotFound()
    {
        _svc.AddVariantOptionResult = Result.Failure(ProductErrors.VariantTypeNotFound);

        var result = await CreateController().AddVariantOptionAsync(999, new CreateVariantOptionRequest("X", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── DeleteVariantOptionAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantOption_Returns200_OnSuccess()
    {
        _svc.DeleteVariantOptionResult = Result.Success();

        var result = await CreateController().DeleteVariantOptionAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task DeleteVariantOption_ReturnsProblem_WhenNotFound()
    {
        _svc.DeleteVariantOptionResult = Result.Failure(ProductErrors.OptionNotFound);

        var result = await CreateController().DeleteVariantOptionAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── RegenerateVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RegenerateVariants_Returns200_OnSuccess()
    {
        _svc.RegenerateResult = Result.Success();

        var result = await CreateController().RegenerateVariantsAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task RegenerateVariants_ReturnsProblem_WhenTypeHasNoOptions()
    {
        _svc.RegenerateResult = Result.Failure(ProductErrors.VariantTypeHasNoOptions);

        var result = await CreateController().RegenerateVariantsAsync(1);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── UpdateVariantAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateVariant_Returns200_OnSuccess()
    {
        _svc.UpdateVariantResult = Result.Success();

        var result = await CreateController().UpdateVariantAsync(1, new UpdateVariantRequest(null, 99m, 10, true, null));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task UpdateVariant_ReturnsProblem_WhenNotFound()
    {
        _svc.UpdateVariantResult = Result.Failure(ProductErrors.VariantNotFound);

        var result = await CreateController().UpdateVariantAsync(999, new UpdateVariantRequest(null, 0, 0, true, null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── DeleteVariantAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariant_Returns200_OnSuccess()
    {
        _svc.DeleteVariantResult = Result.Success();

        var result = await CreateController().DeleteVariantAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task DeleteVariant_ReturnsProblem_WhenNotFound()
    {
        _svc.DeleteVariantResult = Result.Failure(ProductErrors.VariantNotFound);

        var result = await CreateController().DeleteVariantAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── BulkUpdateVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateVariants_Returns200_OnSuccess()
    {
        _svc.BulkUpdateResult = Result.Success();

        var request = new BulkUpdateVariantsRequest(
        [
            new(101, "Red / S", 156800m, 25, true, null),
            new(102, "Blue / M", 156800m, 12, true, null)
        ]);

        var result = await CreateController().BulkUpdateVariantsAsync(1, request);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task BulkUpdateVariants_ReturnsProblem_WhenProductNotFound()
    {
        _svc.BulkUpdateResult = Result.Failure(ProductErrors.NotFound);

        var request = new BulkUpdateVariantsRequest([new(101, null, 0, 0, true, null)]);

        var result = await CreateController().BulkUpdateVariantsAsync(999, request);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task BulkUpdateVariants_ReturnsProblem_WhenVariantNotFound()
    {
        _svc.BulkUpdateResult = Result.Failure(ProductErrors.VariantNotFound);

        var request = new BulkUpdateVariantsRequest([new(999, null, 0, 0, true, null)]);

        var result = await CreateController().BulkUpdateVariantsAsync(1, request);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ──────────────────────────────────────────────────────────

    private class FakeProductService : IProductService
    {
        public Result<List<ProductListResponse>> GetAllResult { get; set; } = Result<List<ProductListResponse>>.Success([]);
        public Result<ProductAdminDetailResponse> GetByIdResult { get; set; } = Result<ProductAdminDetailResponse>.Success(
            new(1, 1, "Cat", "Prod", null, SyncSource.Manual, true, [], [], DateTime.UtcNow, null));
        public Result<long> CreateResult { get; set; } = Result<long>.Success(1);
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();
        public Result ToggleResult { get; set; } = Result.Success();
        public Result AddVariantTypeResult { get; set; } = Result.Success();
        public Result DeleteVariantTypeResult { get; set; } = Result.Success();
        public Result AddVariantOptionResult { get; set; } = Result.Success();
        public Result DeleteVariantOptionResult { get; set; } = Result.Success();
        public Result RegenerateResult { get; set; } = Result.Success();
        public Result BulkUpdateResult { get; set; } = Result.Success();
        public Result UpdateVariantResult { get; set; } = Result.Success();
        public Result DeleteVariantResult { get; set; } = Result.Success();

        public Task<Result<List<ProductListResponse>>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(GetAllResult);
        public Task<Result<ProductAdminDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetByIdResult);
        public Task<Result<long>> CreateAsync(CreateProductRequest r, CancellationToken ct = default) => Task.FromResult(CreateResult);
        public Task<Result> UpdateAsync(long id, UpdateProductRequest r, CancellationToken ct = default) => Task.FromResult(UpdateResult);
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteResult);
        public Task<Result> ToggleActiveAsync(long id, CancellationToken ct = default) => Task.FromResult(ToggleResult);
        public Task<Result> AddVariantTypeAsync(long productId, CreateVariantTypeRequest r, CancellationToken ct = default) => Task.FromResult(AddVariantTypeResult);
        public Task<Result> DeleteVariantTypeAsync(long typeId, CancellationToken ct = default) => Task.FromResult(DeleteVariantTypeResult);
        public Task<Result> AddVariantOptionAsync(long typeId, CreateVariantOptionRequest r, CancellationToken ct = default) => Task.FromResult(AddVariantOptionResult);
        public Task<Result> DeleteVariantOptionAsync(long optionId, CancellationToken ct = default) => Task.FromResult(DeleteVariantOptionResult);
        public Task<Result> RegenerateVariantsAsync(long productId, CancellationToken ct = default) => Task.FromResult(RegenerateResult);
        public Task<Result> BulkUpdateVariantsAsync(long productId, BulkUpdateVariantsRequest r, CancellationToken ct = default) => Task.FromResult(BulkUpdateResult);
        public Task<Result> UpdateVariantAsync(long variantId, UpdateVariantRequest r, CancellationToken ct = default) => Task.FromResult(UpdateVariantResult);
        public Task<Result> DeleteVariantAsync(long variantId, CancellationToken ct = default) => Task.FromResult(DeleteVariantResult);
    }
}
