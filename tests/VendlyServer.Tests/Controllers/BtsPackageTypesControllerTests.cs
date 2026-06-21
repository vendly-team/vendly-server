using Microsoft.AspNetCore.Http.HttpResults;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class BtsPackageTypesControllerTests
{
    private readonly FakeBtsRefService _svc = new();

    private BtsPackageTypesController CreateController() => new BtsPackageTypesController(_svc).WithContext();

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllPackageTypesResult = Result<List<BtsPackageTypeResponse>>.Success(
        [
            new() { Id = 1, BtsId = 10, Name = "Standard" }
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<BtsPackageTypeResponse>>>(result);
        Assert.Single(ok.Value!);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllPackageTypesResult = Result<List<BtsPackageTypeResponse>>.Failure(BtsRefErrors.PackageTypeNotFound);

        var result = await CreateController().GetAllAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetPackageTypeByIdResult = Result<BtsPackageTypeResponse>.Success(new() { Id = 1, BtsId = 10, Name = "Standard" });

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<BtsPackageTypeResponse>>(result);
        Assert.Equal("Standard", ok.Value!.Name);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetPackageTypeByIdResult = Result<BtsPackageTypeResponse>.Failure(BtsRefErrors.PackageTypeNotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddPackageTypeResult = Result.Success();

        var result = await CreateController().AddAsync(new SaveBtsPackageTypeRequest { BtsId = 99, Name = "X" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnConflict()
    {
        _svc.AddPackageTypeResult = Result.Failure(BtsRefErrors.PackageTypeBtsIdExists);

        var result = await CreateController().AddAsync(new SaveBtsPackageTypeRequest { BtsId = 10, Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdatePackageTypeResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new SaveBtsPackageTypeRequest { BtsId = 10, Name = "New" });

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdatePackageTypeResult = Result.Failure(BtsRefErrors.PackageTypeNotFound);

        var result = await CreateController().UpdateAsync(999, new SaveBtsPackageTypeRequest { BtsId = 10, Name = "X" });

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeletePackageTypeResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeletePackageTypeResult = Result.Failure(BtsRefErrors.PackageTypeNotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }
}
