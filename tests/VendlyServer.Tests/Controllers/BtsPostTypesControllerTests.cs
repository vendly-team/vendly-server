using Microsoft.AspNetCore.Http.HttpResults;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class BtsPostTypesControllerTests
{
    private readonly FakeBtsRefService _svc = new();

    private BtsPostTypesController CreateController() => new BtsPostTypesController(_svc).WithContext();

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllPostTypesResult = Result<List<BtsPostTypeResponse>>.Success(
        [
            new() { Id = 1, BtsId = 20, Name = "Express" }
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<BtsPostTypeResponse>>>(result);
        Assert.Single(ok.Value!);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllPostTypesResult = Result<List<BtsPostTypeResponse>>.Failure(BtsRefErrors.PostTypeNotFound);

        var result = await CreateController().GetAllAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetPostTypeByIdResult = Result<BtsPostTypeResponse>.Success(new() { Id = 1, BtsId = 20, Name = "Express" });

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<BtsPostTypeResponse>>(result);
        Assert.Equal("Express", ok.Value!.Name);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetPostTypeByIdResult = Result<BtsPostTypeResponse>.Failure(BtsRefErrors.PostTypeNotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddPostTypeResult = Result.Success();

        var result = await CreateController().AddAsync(new SaveBtsPostTypeRequest { BtsId = 99, Name = "X" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnConflict()
    {
        _svc.AddPostTypeResult = Result.Failure(BtsRefErrors.PostTypeBtsIdExists);

        var result = await CreateController().AddAsync(new SaveBtsPostTypeRequest { BtsId = 20, Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdatePostTypeResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new SaveBtsPostTypeRequest { BtsId = 20, Name = "New" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdatePostTypeResult = Result.Failure(BtsRefErrors.PostTypeNotFound);

        var result = await CreateController().UpdateAsync(999, new SaveBtsPostTypeRequest { BtsId = 20, Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeletePostTypeResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeletePostTypeResult = Result.Failure(BtsRefErrors.PostTypeNotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }
}
