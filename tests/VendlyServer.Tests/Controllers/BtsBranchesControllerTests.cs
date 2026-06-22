using Microsoft.AspNetCore.Http.HttpResults;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class BtsBranchesControllerTests
{
    private readonly FakeBtsRefService _svc = new();

    private BtsBranchesController CreateController() => new BtsBranchesController(_svc).WithContext();

    [Fact]
    public async Task GetAll_NoFilter_UsesGetAll_ReturnsOk()
    {
        _svc.GetAllBranchesResult = Result<List<BtsBranchResponse>>.Success(
        [
            new() { Id = 1, RegionCode = "R01", CityCode = "C01", Code = "B01", Name = "Filial 1", Address = "A" }
        ]);

        var result = await CreateController().GetAllAsync(regionCode: null, cityCode: null);

        var ok = Assert.IsType<Ok<List<BtsBranchResponse>>>(result);
        Assert.Single(ok.Value!);
        Assert.True(_svc.AllBranchesCalled);
    }

    [Fact]
    public async Task GetAll_WithRegionCode_UsesByRegion()
    {
        _svc.GetBranchesByRegionResult = Result<List<BtsBranchResponse>>.Success([]);

        var result = await CreateController().GetAllAsync(regionCode: "R01", cityCode: null);

        Assert.IsType<Ok<List<BtsBranchResponse>>>(result);
        Assert.True(_svc.BranchesByRegionCalled);
        Assert.False(_svc.BranchesByCityCalled);
        Assert.False(_svc.AllBranchesCalled);
    }

    [Fact]
    public async Task GetAll_WithCityCode_UsesByCity()
    {
        _svc.GetBranchesByCityResult = Result<List<BtsBranchResponse>>.Success([]);

        var result = await CreateController().GetAllAsync(regionCode: null, cityCode: "C01");

        Assert.IsType<Ok<List<BtsBranchResponse>>>(result);
        Assert.True(_svc.BranchesByCityCalled);
        Assert.False(_svc.BranchesByRegionCalled);
        Assert.False(_svc.AllBranchesCalled);
    }

    [Fact]
    public async Task GetAll_RegionTakesPrecedenceOverCity()
    {
        _svc.GetBranchesByRegionResult = Result<List<BtsBranchResponse>>.Success([]);

        var result = await CreateController().GetAllAsync(regionCode: "R01", cityCode: "C01");

        Assert.IsType<Ok<List<BtsBranchResponse>>>(result);
        Assert.True(_svc.BranchesByRegionCalled);
        Assert.False(_svc.BranchesByCityCalled);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllBranchesResult = Result<List<BtsBranchResponse>>.Failure(BtsRefErrors.BranchNotFound);

        var result = await CreateController().GetAllAsync(regionCode: null, cityCode: null);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetBranchByIdResult = Result<BtsBranchResponse>.Success(new() { Id = 1, Code = "B01", Name = "Filial 1" });

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<BtsBranchResponse>>(result);
        Assert.Equal("B01", ok.Value!.Code);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetBranchByIdResult = Result<BtsBranchResponse>.Failure(BtsRefErrors.BranchNotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddBranchResult = Result.Success();

        var result = await CreateController().AddAsync(new SaveBtsBranchRequest { RegionCode = "R01", CityCode = "C01", Code = "B99", Name = "X", Address = "A" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnConflict()
    {
        _svc.AddBranchResult = Result.Failure(BtsRefErrors.BranchCodeExists);

        var result = await CreateController().AddAsync(new SaveBtsBranchRequest { RegionCode = "R01", CityCode = "C01", Code = "B01", Name = "X", Address = "A" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateBranchResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new SaveBtsBranchRequest { RegionCode = "R01", CityCode = "C01", Code = "B01", Name = "New", Address = "A" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateBranchResult = Result.Failure(BtsRefErrors.BranchNotFound);

        var result = await CreateController().UpdateAsync(999, new SaveBtsBranchRequest { RegionCode = "R01", CityCode = "C01", Code = "BXX", Name = "X", Address = "A" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteBranchResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteBranchResult = Result.Failure(BtsRefErrors.BranchNotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }
}
