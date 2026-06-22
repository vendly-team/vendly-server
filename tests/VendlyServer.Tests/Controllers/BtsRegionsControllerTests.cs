using Microsoft.AspNetCore.Http.HttpResults;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class BtsRegionsControllerTests
{
    private readonly FakeBtsRefService _svc = new();

    private BtsRegionsController CreateController() => new BtsRegionsController(_svc).WithContext();

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllRegionsResult = Result<List<BtsRegionResponse>>.Success(
        [
            new() { Id = 1, Code = "R01", Name = "Toshkent" },
            new() { Id = 2, Code = "R02", Name = "Andijon" }
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<BtsRegionResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetRegionByIdResult = Result<BtsRegionResponse>.Success(new() { Id = 1, Code = "R01", Name = "Toshkent" });

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<BtsRegionResponse>>(result);
        Assert.Equal("R01", ok.Value!.Code);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetRegionByIdResult = Result<BtsRegionResponse>.Failure(BtsRefErrors.RegionNotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddRegionResult = Result.Success();

        var result = await CreateController().AddAsync(new SaveBtsRegionRequest { Code = "R99", Name = "X" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnConflict()
    {
        _svc.AddRegionResult = Result.Failure(BtsRefErrors.RegionCodeExists);

        var result = await CreateController().AddAsync(new SaveBtsRegionRequest { Code = "R01", Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateRegionResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new SaveBtsRegionRequest { Code = "R01", Name = "New" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateRegionResult = Result.Failure(BtsRefErrors.RegionNotFound);

        var result = await CreateController().UpdateAsync(999, new SaveBtsRegionRequest { Code = "RXX", Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteRegionResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteRegionResult = Result.Failure(BtsRefErrors.RegionNotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }
}
