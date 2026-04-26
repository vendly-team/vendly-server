using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.Category.Contracts;
using VendlyServer.Application.Services.Storage;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Category;

public class CategoryService(
    AppDbContext dbContext,
    IStorageService storageService,
    ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<Result<List<CategoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.ImageUrl, c.IsActive, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.ImageUrl, c.IsActive, c.CreatedAt, c.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? CategoryErrors.NotFound : category;
    }

    public async Task<Result> AddAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Name == request.Name, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                return CategoryErrors.AlreadyExists;

            existing.IsDeleted = false;

            if (request.Image is not null)
            {
                var upload = await storageService.UploadAsync(request.Image, "categories", cancellationToken);
                if (!upload.IsSuccess) return upload.Error;
                existing.ImageUrl = upload.Data;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        string? imageUrl = null;

        if (request.Image is not null)
        {
            var upload = await storageService.UploadAsync(request.Image, "categories", cancellationToken);
            if (!upload.IsSuccess) return upload.Error;
            imageUrl = upload.Data;
        }

        dbContext.Categories.Add(new Domain.Entities.Catalogs.Category
        {
            Name = request.Name,
            ImageUrl = imageUrl
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null) return CategoryErrors.NotFound;

        string? imageUrl = category.ImageUrl;

        if (request.Image is not null)
        {
            var upload = await storageService.UploadAsync(request.Image, "categories", cancellationToken);
            if (!upload.IsSuccess) return upload.Error;

            var oldUrl = category.ImageUrl;
            imageUrl = upload.Data;
            category.ImageUrl = imageUrl;
            category.Name = request.Name;

            await dbContext.SaveChangesAsync(cancellationToken);

            if (oldUrl is not null)
                await TryDeleteFileAsync(oldUrl);
        }
        else
        {
            category.Name = request.Name;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null) return CategoryErrors.NotFound;

        var imageUrl = category.ImageUrl;

        category.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (imageUrl is not null)
            await TryDeleteFileAsync(imageUrl);

        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null) return CategoryErrors.NotFound;

        category.IsActive = !category.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task TryDeleteFileAsync(string fileUrl)
    {
        var result = await storageService.DeleteAsync(fileUrl);
        if (!result.IsSuccess)
            logger.LogWarning("Failed to delete category image: {Url}", fileUrl);
    }
}
