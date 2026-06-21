using Microsoft.AspNetCore.Http.HttpResults;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class BtsCitiesControllerTests
{
    private readonly FakeBtsRefService _svc = new();

    private BtsCitiesController CreateController() => new BtsCitiesController(_svc).WithContext();

    [Fact]
    public async Task GetAll_NoFilter_UsesGetAll_ReturnsOk()
    {
        _svc.GetAllCitiesResult = Result<List<BtsCityResponse>>.Success(
        [
            new() { Id = 1, RegionCode = "R01", Code = "C01", Name = "Toshkent sh." }
        ]);

        var result = await CreateController().GetAllAsync(regionCode: null);

        var ok = Assert.IsType<Ok<List<BtsCityResponse>>>(result);
        Assert.Single(ok.Value!);
        Assert.True(_svc.AllCitiesCalled);
        Assert.False(_svc.CitiesByRegionCalled);
    }

    [Fact]
    public async Task GetAll_WithRegionCode_UsesByRegion_ReturnsOk()
    {
        _svc.GetCitiesByRegionResult = Result<List<BtsCityResponse>>.Success(
        [
            new() { Id = 1, RegionCode = "R01", Code = "C01", Name = "Toshkent sh." },
            new() { Id = 2, RegionCode = "R01", Code = "C02", Name = "Chirchiq" }
        ]);

        var result = await CreateController().GetAllAsync(regionCode: "R01");

        var ok = Assert.IsType<Ok<List<BtsCityResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
        Assert.True(_svc.CitiesByRegionCalled);
        Assert.False(_svc.AllCitiesCalled);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllCitiesResult = Result<List<BtsCityResponse>>.Failure(BtsRefErrors.CityNotFound);

        var result = await CreateController().GetAllAsync(regionCode: null);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetCityByIdResult = Result<BtsCityResponse>.Success(new() { Id = 1, Code = "C01", Name = "Toshkent sh." });

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<BtsCityResponse>>(result);
        Assert.Equal("C01", ok.Value!.Code);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetCityByIdResult = Result<BtsCityResponse>.Failure(BtsRefErrors.CityNotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddCityResult = Result.Success();

        var result = await CreateController().AddAsync(new SaveBtsCityRequest { RegionCode = "R01", Code = "C99", Name = "X" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnConflict()
    {
        _svc.AddCityResult = Result.Failure(BtsRefErrors.CityCodeExists);

        var result = await CreateController().AddAsync(new SaveBtsCityRequest { RegionCode = "R01", Code = "C01", Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateCityResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new SaveBtsCityRequest { RegionCode = "R01", Code = "C01", Name = "New" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateCityResult = Result.Failure(BtsRefErrors.CityNotFound);

        var result = await CreateController().UpdateAsync(999, new SaveBtsCityRequest { RegionCode = "R01", Code = "CXX", Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteCityResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteCityResult = Result.Failure(BtsRefErrors.CityNotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }
}
