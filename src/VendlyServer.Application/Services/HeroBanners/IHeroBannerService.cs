using VendlyServer.Application.Services.HeroBanners.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.HeroBanners;

public interface IHeroBannerService
{
    /// <summary>Storefront — only active banners, sorted by SortOrder.</summary>
    Task<Result<List<HeroBannerResponse>>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Admin — all banners (including inactive), sorted by SortOrder.</summary>
    Task<Result<List<HeroBannerResponse>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<HeroBannerResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<HeroBannerResponse>> CreateAsync(CreateHeroBannerRequest request, CancellationToken cancellationToken = default);
    Task<Result<HeroBannerResponse>> UpdateAsync(long id, UpdateHeroBannerRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
