using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.Category;
using VendlyServer.Application.Services.Category.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly FakeCategoryService _svc = new();

    private CategoriesController CreateController()
    {
        var ctrl = new CategoriesController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllResult = Result<List<CategoryResponse>>.Success(
        [
            new(1, "Electronics", null, true, DateTime.UtcNow, null),
            new(2, "Clothing", null, false, DateTime.UtcNow, null)
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<CategoryResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<CategoryResponse>.Success(
            new(1, "Electronics", "img.png", true, DateTime.UtcNow, null));

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<CategoryResponse>>(result);
        Assert.Equal("Electronics", ok.Value!.Name);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdResult = Result<CategoryResponse>.Failure(CategoryErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddResult = Result.Success();

        var result = await CreateController().AddAsync(new CreateCategoryRequest("Books", null));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnAlreadyExists()
    {
        _svc.AddResult = Result.Failure(CategoryErrors.AlreadyExists);

        var result = await CreateController().AddAsync(new CreateCategoryRequest("Electronics", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new UpdateCategoryRequest("New Name", null));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateResult = Result.Failure(CategoryErrors.NotFound);

        var result = await CreateController().UpdateAsync(999, new UpdateCategoryRequest("X", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

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
        _svc.DeleteResult = Result.Failure(CategoryErrors.NotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Toggle_ReturnsOk_OnSuccess()
    {
        _svc.ToggleResult = Result.Success();

        var result = await CreateController().ToggleActiveAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Toggle_ReturnsProblem_OnNotFound()
    {
        _svc.ToggleResult = Result.Failure(CategoryErrors.NotFound);

        var result = await CreateController().ToggleActiveAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    private class FakeCategoryService : ICategoryService
    {
        public Result<List<CategoryResponse>> GetAllResult { get; set; } = Result<List<CategoryResponse>>.Success([]);
        public Result<CategoryResponse> GetByIdResult { get; set; } = Result<CategoryResponse>.Success(new(1, "Test", null, true, DateTime.UtcNow, null));
        public Result AddResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();
        public Result ToggleResult { get; set; } = Result.Success();

        public Task<Result<List<CategoryResponse>>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(GetAllResult);
        public Task<Result<CategoryResponse>> GetByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetByIdResult);
        public Task<Result> AddAsync(CreateCategoryRequest r, CancellationToken ct = default) => Task.FromResult(AddResult);
        public Task<Result> UpdateAsync(long id, UpdateCategoryRequest r, CancellationToken ct = default) => Task.FromResult(UpdateResult);
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteResult);
        public Task<Result> ToggleActiveAsync(long id, CancellationToken ct = default) => Task.FromResult(ToggleResult);
    }
}
