using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.HeroBanners.Contracts;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;
using HeroBannerEntity = VendlyServer.Domain.Entities.Public.HeroBanner;

namespace VendlyServer.Application.Services.HeroBanners;

public class HeroBannerService(
    AppDbContext dbContext,
    IStorageService storageService,
    ILogger<HeroBannerService> logger) : IHeroBannerService
{
    public async Task<Result<List<HeroBannerResponse>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var banners = await dbContext.HeroBanners
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);

        return Result<List<HeroBannerResponse>>.Success(banners.Select(MapToResponse).ToList());
    }

    public async Task<Result<List<HeroBannerResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var banners = await dbContext.HeroBanners
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);

        return Result<List<HeroBannerResponse>>.Success(banners.Select(MapToResponse).ToList());
    }

    public async Task<Result<HeroBannerResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.HeroBanners
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);

        return banner is null
            ? Result<HeroBannerResponse>.Failure(Error.NotFound("HeroBanner.NotFound"))
            : MapToResponse(banner);
    }

    public async Task<Result<HeroBannerResponse>> CreateAsync(
        CreateHeroBannerRequest request, CancellationToken cancellationToken = default)
    {
        var banner = new HeroBannerEntity
        {
            Title = request.Title,
            Subtitle = request.Subtitle,
            BadgeText = request.BadgeText,
            CtaText = request.CtaText,
            CtaLink = request.CtaLink,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
        };

        // Image upload
        var image = await ReplaceFileAsync(request.Image, null, "banners", cancellationToken);
        if (!image.IsSuccess) return image.Error;
        banner.ImageUrl = image.Data;

        dbContext.HeroBanners.Add(banner);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(banner);
    }

    public async Task<Result<HeroBannerResponse>> UpdateAsync(
        long id, UpdateHeroBannerRequest request, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.HeroBanners
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);

        if (banner is null)
            return Result<HeroBannerResponse>.Failure(Error.NotFound("HeroBanner.NotFound"));

        banner.Title = request.Title;
        banner.Subtitle = request.Subtitle;
        banner.BadgeText = request.BadgeText;
        banner.CtaText = request.CtaText;
        banner.CtaLink = request.CtaLink;
        banner.SortOrder = request.SortOrder;
        banner.IsActive = request.IsActive;

        // Image replace
        var image = await ReplaceFileAsync(request.Image, banner.ImageUrl, "banners", cancellationToken);
        if (!image.IsSuccess) return image.Error;
        banner.ImageUrl = image.Data;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(banner);
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.HeroBanners
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);

        if (banner is null)
            return Result.Failure(Error.NotFound("HeroBanner.NotFound"));

        // Soft-delete
        banner.IsDeleted = true;
        banner.IsActive = false;

        // Delete image from storage
        if (banner.ImageUrl is not null)
            await TryDeleteFileAsync(banner.ImageUrl);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.HeroBanners
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);

        if (banner is null)
            return Result.Failure(Error.NotFound("HeroBanner.NotFound"));

        banner.IsActive = !banner.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ── Helpers ──

    /// <summary>If a new file is provided, upload it and delete the old one; otherwise keep the existing URL.</summary>
    private async Task<Result<string?>> ReplaceFileAsync(
        Microsoft.AspNetCore.Http.IFormFile? file, string? oldUrl, string folder, CancellationToken cancellationToken)
    {
        if (file is null) return Result<string?>.Success(oldUrl);

        var upload = await storageService.UploadAsync(file, folder, cancellationToken);
        if (!upload.IsSuccess) return upload.Error;

        if (oldUrl is not null)
            await TryDeleteFileAsync(oldUrl);

        return Result<string?>.Success(upload.Data);
    }

    private async Task TryDeleteFileAsync(string fileUrl)
    {
        var result = await storageService.DeleteAsync(fileUrl);
        if (!result.IsSuccess)
            logger.LogWarning("Failed to delete banner image: {Url}", fileUrl);
    }

    private static HeroBannerResponse MapToResponse(HeroBannerEntity b) => new(
        b.Id,
        b.Title,
        b.Subtitle,
        b.BadgeText,
        b.CtaText,
        b.CtaLink,
        b.ImageUrl,
        b.SortOrder,
        b.IsActive,
        b.CreatedAt,
        b.UpdatedAt);
}
