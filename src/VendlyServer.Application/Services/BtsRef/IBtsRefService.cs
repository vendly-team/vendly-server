using VendlyServer.Application.Services.BtsRef.Contracts;

namespace VendlyServer.Application.Services.BtsRef;

public interface IBtsRefService
{
    // Regions
    Task<Result<List<BtsRegionResponse>>> GetAllRegionsAsync(CancellationToken cancellationToken = default);
    Task<Result<BtsRegionResponse>> GetRegionByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddRegionAsync(SaveBtsRegionRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateRegionAsync(long id, SaveBtsRegionRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRegionAsync(long id, CancellationToken cancellationToken = default);

    // Cities
    Task<Result<List<BtsCityResponse>>> GetAllCitiesAsync(CancellationToken cancellationToken = default);
    Task<Result<BtsCityResponse>> GetCityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddCityAsync(SaveBtsCityRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateCityAsync(long id, SaveBtsCityRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCityAsync(long id, CancellationToken cancellationToken = default);

    // Branches
    Task<Result<List<BtsBranchResponse>>> GetAllBranchesAsync(CancellationToken cancellationToken = default);
    Task<Result<BtsBranchResponse>> GetBranchByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddBranchAsync(SaveBtsBranchRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateBranchAsync(long id, SaveBtsBranchRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteBranchAsync(long id, CancellationToken cancellationToken = default);

    // Package Types
    Task<Result<List<BtsPackageTypeResponse>>> GetAllPackageTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<BtsPackageTypeResponse>> GetPackageTypeByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddPackageTypeAsync(SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdatePackageTypeAsync(long id, SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeletePackageTypeAsync(long id, CancellationToken cancellationToken = default);

    // Post Types
    Task<Result<List<BtsPostTypeResponse>>> GetAllPostTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<BtsPostTypeResponse>> GetPostTypeByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddPostTypeAsync(SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdatePostTypeAsync(long id, SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeletePostTypeAsync(long id, CancellationToken cancellationToken = default);
}
