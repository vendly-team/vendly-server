using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.CategoryPrices;
using VendlyServer.Application.Services.CategoryPrices.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Tests.Controllers;

public class CategoryPricesControllerTests
{
    private readonly FakeCategoryPriceService _svc = new();

    private CategoryPricesController CreateController()
    {
        var ctrl = new CategoryPricesController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static CreateCategoryPriceRequest CreateRequest() =>
        new(1, PriceMarkupType.Percent, 15m, 1_000m, null, null);

    private static UpdateCategoryPriceRequest UpdateRequest() =>
        new(1, PriceMarkupType.Percent, 15m, 1_000m, null, null);

    private static CategoryPriceResponse SampleResponse(long id = 1) =>
        new(id, 1, PriceMarkupType.Percent, 15m, 1_000m, null, null, DateTime.UtcNow, null);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllResult = Result<List<CategoryPriceResponse>>.Success(
            [SampleResponse(1), SampleResponse(2)]);

        var result = await CreateController().GetAllAsync(null);

        var ok = Assert.IsType<Ok<List<CategoryPriceResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllResult = Result<List<CategoryPriceResponse>>.Failure(CategoryPriceErrors.NotFound);

        var result = await CreateController().GetAllAsync(null);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<CategoryPriceResponse>.Success(SampleResponse(5));

        var result = await CreateController().GetByIdAsync(5);

        var ok = Assert.IsType<Ok<CategoryPriceResponse>>(result);
        Assert.Equal(5, ok.Value!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdResult = Result<CategoryPriceResponse>.Failure(CategoryPriceErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_ReturnsOkWithId_OnSuccess()
    {
        _svc.AddResult = Result<long>.Success(42);

        var result = await CreateController().AddAsync(CreateRequest());

        var ok = Assert.IsType<Ok<long>>(result);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnCategoryNotFound()
    {
        _svc.AddResult = Result<long>.Failure(CategoryPriceErrors.CategoryNotFound);

        var result = await CreateController().AddAsync(CreateRequest());

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, UpdateRequest());

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateResult = Result.Failure(CategoryPriceErrors.NotFound);

        var result = await CreateController().UpdateAsync(999, UpdateRequest());

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteResult = Result.Failure(CategoryPriceErrors.NotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    private sealed class FakeCategoryPriceService : ICategoryPriceService
    {
        public Result<List<CategoryPriceResponse>> GetAllResult { get; set; } =
            Result<List<CategoryPriceResponse>>.Success([]);
        public Result<CategoryPriceResponse> GetByIdResult { get; set; } =
            Result<CategoryPriceResponse>.Success(
                new(1, 1, PriceMarkupType.Percent, 15m, null, null, null, DateTime.UtcNow, null));
        public Result<long> AddResult { get; set; } = Result<long>.Success(1);
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();

        public Task<Result<List<CategoryPriceResponse>>> GetAllAsync(long? categoryId, CancellationToken ct = default)
            => Task.FromResult(GetAllResult);
        public Task<Result<CategoryPriceResponse>> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(GetByIdResult);
        public Task<Result<long>> AddAsync(CreateCategoryPriceRequest r, CancellationToken ct = default)
            => Task.FromResult(AddResult);
        public Task<Result> UpdateAsync(long id, UpdateCategoryPriceRequest r, CancellationToken ct = default)
            => Task.FromResult(UpdateResult);
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default)
            => Task.FromResult(DeleteResult);
    }
}
