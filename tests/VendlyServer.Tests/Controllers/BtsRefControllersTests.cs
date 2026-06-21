using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Ref;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

/// <summary>
/// Shared fake for the five Ref controllers that all depend on IBtsRefService.
/// Each result is settable; defaults are success.
/// </summary>
internal sealed class FakeBtsRefService : IBtsRefService
{
    public Result<List<BtsRegionResponse>> GetAllRegionsResult { get; set; } = Result<List<BtsRegionResponse>>.Success([]);
    public Result<BtsRegionResponse> GetRegionByIdResult { get; set; } = Result<BtsRegionResponse>.Success(new());
    public Result AddRegionResult { get; set; } = Result.Success();
    public Result UpdateRegionResult { get; set; } = Result.Success();
    public Result DeleteRegionResult { get; set; } = Result.Success();

    public Result<List<BtsCityResponse>> GetAllCitiesResult { get; set; } = Result<List<BtsCityResponse>>.Success([]);
    public Result<List<BtsCityResponse>> GetCitiesByRegionResult { get; set; } = Result<List<BtsCityResponse>>.Success([]);
    public Result<BtsCityResponse> GetCityByIdResult { get; set; } = Result<BtsCityResponse>.Success(new());
    public Result AddCityResult { get; set; } = Result.Success();
    public Result UpdateCityResult { get; set; } = Result.Success();
    public Result DeleteCityResult { get; set; } = Result.Success();

    public Result<List<BtsBranchResponse>> GetAllBranchesResult { get; set; } = Result<List<BtsBranchResponse>>.Success([]);
    public Result<List<BtsBranchResponse>> GetBranchesByCityResult { get; set; } = Result<List<BtsBranchResponse>>.Success([]);
    public Result<List<BtsBranchResponse>> GetBranchesByRegionResult { get; set; } = Result<List<BtsBranchResponse>>.Success([]);
    public Result<BtsBranchResponse> GetBranchByIdResult { get; set; } = Result<BtsBranchResponse>.Success(new());
    public Result AddBranchResult { get; set; } = Result.Success();
    public Result UpdateBranchResult { get; set; } = Result.Success();
    public Result DeleteBranchResult { get; set; } = Result.Success();

    public Result<List<BtsPackageTypeResponse>> GetAllPackageTypesResult { get; set; } = Result<List<BtsPackageTypeResponse>>.Success([]);
    public Result<BtsPackageTypeResponse> GetPackageTypeByIdResult { get; set; } = Result<BtsPackageTypeResponse>.Success(new());
    public Result AddPackageTypeResult { get; set; } = Result.Success();
    public Result UpdatePackageTypeResult { get; set; } = Result.Success();
    public Result DeletePackageTypeResult { get; set; } = Result.Success();

    public Result<List<BtsPostTypeResponse>> GetAllPostTypesResult { get; set; } = Result<List<BtsPostTypeResponse>>.Success([]);
    public Result<BtsPostTypeResponse> GetPostTypeByIdResult { get; set; } = Result<BtsPostTypeResponse>.Success(new());
    public Result AddPostTypeResult { get; set; } = Result.Success();
    public Result UpdatePostTypeResult { get; set; } = Result.Success();
    public Result DeletePostTypeResult { get; set; } = Result.Success();

    // Tracking flags used to assert which overload was invoked.
    public bool BranchesByRegionCalled { get; private set; }
    public bool BranchesByCityCalled { get; private set; }
    public bool AllBranchesCalled { get; private set; }
    public bool CitiesByRegionCalled { get; private set; }
    public bool AllCitiesCalled { get; private set; }

    public Task<Result<List<BtsRegionResponse>>> GetAllRegionsAsync(CancellationToken ct = default) => Task.FromResult(GetAllRegionsResult);
    public Task<Result<BtsRegionResponse>> GetRegionByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetRegionByIdResult);
    public Task<Result> AddRegionAsync(SaveBtsRegionRequest r, CancellationToken ct = default) => Task.FromResult(AddRegionResult);
    public Task<Result> UpdateRegionAsync(long id, SaveBtsRegionRequest r, CancellationToken ct = default) => Task.FromResult(UpdateRegionResult);
    public Task<Result> DeleteRegionAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteRegionResult);

    public Task<Result<List<BtsCityResponse>>> GetAllCitiesAsync(CancellationToken ct = default) { AllCitiesCalled = true; return Task.FromResult(GetAllCitiesResult); }
    public Task<Result<List<BtsCityResponse>>> GetCitiesByRegionAsync(string regionCode, CancellationToken ct = default) { CitiesByRegionCalled = true; return Task.FromResult(GetCitiesByRegionResult); }
    public Task<Result<BtsCityResponse>> GetCityByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetCityByIdResult);
    public Task<Result> AddCityAsync(SaveBtsCityRequest r, CancellationToken ct = default) => Task.FromResult(AddCityResult);
    public Task<Result> UpdateCityAsync(long id, SaveBtsCityRequest r, CancellationToken ct = default) => Task.FromResult(UpdateCityResult);
    public Task<Result> DeleteCityAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteCityResult);

    public Task<Result<List<BtsBranchResponse>>> GetAllBranchesAsync(CancellationToken ct = default) { AllBranchesCalled = true; return Task.FromResult(GetAllBranchesResult); }
    public Task<Result<List<BtsBranchResponse>>> GetBranchesByCityAsync(string cityCode, CancellationToken ct = default) { BranchesByCityCalled = true; return Task.FromResult(GetBranchesByCityResult); }
    public Task<Result<List<BtsBranchResponse>>> GetBranchesByRegionAsync(string regionCode, CancellationToken ct = default) { BranchesByRegionCalled = true; return Task.FromResult(GetBranchesByRegionResult); }
    public Task<Result<BtsBranchResponse>> GetBranchByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetBranchByIdResult);
    public Task<Result> AddBranchAsync(SaveBtsBranchRequest r, CancellationToken ct = default) => Task.FromResult(AddBranchResult);
    public Task<Result> UpdateBranchAsync(long id, SaveBtsBranchRequest r, CancellationToken ct = default) => Task.FromResult(UpdateBranchResult);
    public Task<Result> DeleteBranchAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteBranchResult);

    public Task<Result<List<BtsPackageTypeResponse>>> GetAllPackageTypesAsync(CancellationToken ct = default) => Task.FromResult(GetAllPackageTypesResult);
    public Task<Result<BtsPackageTypeResponse>> GetPackageTypeByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetPackageTypeByIdResult);
    public Task<Result> AddPackageTypeAsync(SaveBtsPackageTypeRequest r, CancellationToken ct = default) => Task.FromResult(AddPackageTypeResult);
    public Task<Result> UpdatePackageTypeAsync(long id, SaveBtsPackageTypeRequest r, CancellationToken ct = default) => Task.FromResult(UpdatePackageTypeResult);
    public Task<Result> DeletePackageTypeAsync(long id, CancellationToken ct = default) => Task.FromResult(DeletePackageTypeResult);

    public Task<Result<List<BtsPostTypeResponse>>> GetAllPostTypesAsync(CancellationToken ct = default) => Task.FromResult(GetAllPostTypesResult);
    public Task<Result<BtsPostTypeResponse>> GetPostTypeByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetPostTypeByIdResult);
    public Task<Result> AddPostTypeAsync(SaveBtsPostTypeRequest r, CancellationToken ct = default) => Task.FromResult(AddPostTypeResult);
    public Task<Result> UpdatePostTypeAsync(long id, SaveBtsPostTypeRequest r, CancellationToken ct = default) => Task.FromResult(UpdatePostTypeResult);
    public Task<Result> DeletePostTypeAsync(long id, CancellationToken ct = default) => Task.FromResult(DeletePostTypeResult);
}

internal static class BtsRefControllerTestHelpers
{
    public static T WithContext<T>(this T ctrl) where T : ControllerBase
    {
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }
}
