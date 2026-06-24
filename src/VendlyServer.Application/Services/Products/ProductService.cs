using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Pricing;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Products;

public class ProductService(
    AppDbContext dbContext,
    IStorageService storageService,
    ILogger<ProductService> logger,
    IProductPricingService pricingService,
    IOptions<ClientOptions> clientOptions) : IProductService
{
    private readonly ClientOptions _clientOptions = clientOptions.Value;

    public async Task<PagedList<ProductCardResponse>> GetAllAsync(ProductFilterRequest request,
        CancellationToken ct = default)
    {
        var baseQuery = dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive)
            .Where(p => request.CategoryId == null || p.CategoryId == request.CategoryId);

        var totalCount = await baseQuery.CountAsync(ct);

        var products = await baseQuery
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => !v.IsDeleted && v.IsActive))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var pricing = await ResolvePricingContextAsync("product list", ct);

        var items = products.Select(p =>
        {
            var variants = p.Variants.ToList();
            var defaultVariant = variants.Count > 0 ? variants.MinBy(v => v.Price) : null;

            decimal? minPrice = defaultVariant is null
                ? null
                : pricing is null
                    ? defaultVariant.Price
                    : pricing.CalculateSoumPrice(defaultVariant.Price, p.CategoryId);

            return new ProductCardResponse(
                p.Id,
                p.Name,
                p.CategoryId,
                p.Category.Name,
                p.Description,
                minPrice,
                variants.Sum(v => v.Quantity),
                variants.Count,
                variants.SelectMany(v => v.Images).FirstOrDefault(),
                (long?)defaultVariant?.Id,
                variants.Count > 0 ? variants.OrderBy(v => v.Id).Select(v => (long?)v.Id).FirstOrDefault() : null);
        }).ToList();

        return new PagedList<ProductCardResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<Result<List<ProductListResponse>>> GetAllAdminAsync(CancellationToken ct = default)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new ProductListResponse(
                p.Id,
                p.CategoryId,
                p.Category.Name,
                p.Name,
                p.Description,
                p.SyncSource,
                p.IsActive,
                p.Variants.Count(v => !v.IsDeleted),
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(ct);

        return products;
    }

    public async Task<Result<List<ProductSearchResponse>>> SearchAsync(string query, CancellationToken ct = default)
    {
        var normalizedQuery = query.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 2)
            return new List<ProductSearchResponse>();

        var clientBaseUrl = (_clientOptions.BaseUrl ?? string.Empty).TrimEnd('/');

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive)
            .Where(p =>
                (p.Name.Uz != null && p.Name.Uz.ToLower().Contains(normalizedQuery)) ||
                (p.Name.Ru != null && p.Name.Ru.ToLower().Contains(normalizedQuery)) ||
                (p.Name.En != null && p.Name.En.ToLower().Contains(normalizedQuery)) ||
                (p.Description != null && p.Description.ToLower().Contains(normalizedQuery)) ||
                (p.Category.Name.Uz != null && p.Category.Name.Uz.ToLower().Contains(normalizedQuery)) ||
                (p.Category.Name.Ru != null && p.Category.Name.Ru.ToLower().Contains(normalizedQuery)) ||
                p.Variants.Any(v =>
                    !v.IsDeleted &&
                    (v.Name != null && v.Name.ToLower().Contains(normalizedQuery))))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.CategoryId,
                RawMinPrice = p.Variants
                    .Where(v => !v.IsDeleted && v.IsActive)
                    .Select(v => (decimal?)v.Price)
                    .Min() ?? 0m,
                SkuCount = p.Variants.Count(v => !v.IsDeleted),
                Images = p.Variants
                    .Where(v => !v.IsDeleted && v.IsActive)
                    .SelectMany(v => v.Images)
                    .Distinct()
                    .ToList(),
                IsAvailableForSale = p.Variants.Any(v => !v.IsDeleted && v.IsActive && v.Quantity > 0)
            })
            .ToListAsync(ct);

        var pricing = await ResolvePricingContextAsync("product search", ct);

        var result = products.Select(p => new ProductSearchResponse(
            p.Id,
            p.Name,
            pricing is null ? p.RawMinPrice : pricing.CalculateSoumPrice(p.RawMinPrice, p.CategoryId),
            p.SkuCount,
            p.Images,
            p.IsAvailableForSale,
            BuildRedirectUrl(p.Name.Uz ?? p.Name.Ru ?? p.Name.En ?? string.Empty, p.Id, clientBaseUrl)))
            .ToList();

        return result;
    }

    public async Task<Result<ProductAdminDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ProductAdminDetailResponse(
                p.Id,
                p.CategoryId,
                p.Category.Name,
                p.Name,
                p.Description,
                p.SyncSource,
                p.IsActive,
                p.VariantTypes
                    .Where(vt => !vt.IsDeleted)
                    .OrderBy(vt => vt.DisplayOrder)
                    .Select(vt => new VariantTypeResponse(
                        vt.Id,
                        vt.ProductId,
                        vt.Name,
                        vt.DisplayOrder,
                        vt.Options
                            .Where(o => !o.IsDeleted)
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new VariantOptionResponse(
                                o.Id,
                                o.VariantTypeId,
                                o.Name,
                                o.ImageUrl,
                                o.DisplayOrder))
                            .ToList()))
                    .ToList(),
                p.Variants
                    .Where(v => !v.IsDeleted)
                    .Select(v => new ProductVariantResponse(
                        v.Id,
                        v.ProductId,
                        v.Name,
                        v.Price,
                        v.Quantity,
                        v.IsActive,
                        v.Images,
                        v.OptionValues
                            .Select(ov => new VariantCombinationItem(
                                ov.VariantOptionId,
                                ov.VariantOption.Name,
                                ov.VariantOption.VariantType.Name))
                            .ToList()))
                    .ToList(),
                p.CreatedAt,
                p.UpdatedAt))
            .SingleOrDefaultAsync(ct);

        if (product is null) return ProductErrors.NotFound;

        var pricing = await ResolvePricingContextAsync($"product {id} detail", ct);
        if (pricing is not null)
        {
            var repricedVariants = product.Variants
                .Select(v => v with { Price = pricing.CalculateSoumPrice(v.Price, product.CategoryId) })
                .ToList();

            product = product with { Variants = repricedVariants };
        }

        return product;
    }

    public async Task<Result<long>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            SyncSource = request.SyncSource,
            IsActive = true
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task<Result> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null) return ProductErrors.NotFound;

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.IsActive = request.IsActive;
        product.SyncSource = request.SyncSource;

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null) return ProductErrors.NotFound;

        product.IsDeleted = true;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null) return ProductErrors.NotFound;

        product.IsActive = !product.IsActive;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddVariantTypeAsync(long productId, CreateVariantTypeRequest request,
        CancellationToken ct = default)
    {
        var exists = await dbContext.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (!exists) return ProductErrors.NotFound;

        dbContext.VariantTypes.Add(new VariantType
        {
            ProductId = productId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        });

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteVariantTypeAsync(long typeId, CancellationToken ct = default)
    {
        var variantType = await dbContext.VariantTypes
            .SingleOrDefaultAsync(vt => vt.Id == typeId && !vt.IsDeleted, ct);

        if (variantType is null) return ProductErrors.VariantTypeNotFound;

        variantType.IsDeleted = true;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddVariantOptionAsync(long typeId, CreateVariantOptionRequest request,
        CancellationToken ct = default)
    {
        var exists = await dbContext.VariantTypes
            .AnyAsync(vt => vt.Id == typeId && !vt.IsDeleted, ct);

        if (!exists) return ProductErrors.VariantTypeNotFound;

        string? imageUrl = null;

        if (request.Image is not null)
        {
            var upload = await storageService.UploadAsync(request.Image, "variant-options", ct);
            if (!upload.IsSuccess) return upload.Error;
            imageUrl = upload.Data;
        }

        dbContext.VariantOptions.Add(new VariantOption
        {
            VariantTypeId = typeId,
            Name = request.Name,
            ImageUrl = imageUrl,
            DisplayOrder = request.DisplayOrder
        });

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteVariantOptionAsync(long optionId, CancellationToken ct = default)
    {
        var option = await dbContext.VariantOptions
            .SingleOrDefaultAsync(o => o.Id == optionId && !o.IsDeleted, ct);

        if (option is null) return ProductErrors.OptionNotFound;

        var imageUrl = option.ImageUrl;
        option.IsDeleted = true;
        await dbContext.SaveChangesAsync(ct);

        if (imageUrl is not null)
            await TryDeleteFileAsync(imageUrl);

        return Result.Success();
    }

    public async Task<Result> RegenerateVariantsAsync(long productId, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Include(p => p.VariantTypes.Where(vt => !vt.IsDeleted))
            .ThenInclude(vt => vt.Options.Where(o => !o.IsDeleted))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .ThenInclude(v => v.OptionValues)
            .SingleOrDefaultAsync(ct);

        if (product is null) return ProductErrors.NotFound;

        var activeTypes = product.VariantTypes.ToList();

        if (activeTypes.Any(t => !t.Options.Any()))
            return ProductErrors.VariantTypeHasNoOptions;

        var optionSets = activeTypes.Select(t => t.Options.ToList()).ToList();
        var combinations = CartesianProduct(optionSets);

        var existingVariants = product.Variants.ToList();
        var usedVariantIds = new HashSet<long>();

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        foreach (var combo in combinations)
        {
            var comboIds = combo.Select(o => o.Id).OrderBy(x => x).ToList();
            var fingerprint = string.Join(",", comboIds);

            var existing = existingVariants.FirstOrDefault(v =>
            {
                var vIds = v.OptionValues.Select(ov => ov.VariantOptionId).OrderBy(x => x).ToList();
                return string.Join(",", vIds) == fingerprint;
            });

            if (existing is not null)
            {
                usedVariantIds.Add(existing.Id);
                continue;
            }

            var name = string.Join(" / ", combo.Select(o => o.Name));
            var newVariant = new ProductVariant
            {
                ProductId = productId,
                Name = name,
                Price = 0,
                Quantity = 0,
                IsActive = true,
                Images = new List<string>()
            };

            dbContext.ProductVariants.Add(newVariant);
            await dbContext.SaveChangesAsync(ct);

            foreach (var opt in combo)
            {
                dbContext.VariantOptionValues.Add(new VariantOptionValue
                {
                    ProductVariantId = newVariant.Id,
                    VariantOptionId = opt.Id
                });
            }
        }

        foreach (var v in existingVariants.Where(v => !usedVariantIds.Contains(v.Id)))
            v.IsDeleted = true;

        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success();
    }

    public async Task<Result> BulkUpdateVariantsAsync(long productId, BulkUpdateVariantsRequest request,
        CancellationToken ct = default)
    {
        var productExists = await dbContext.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (!productExists) return ProductErrors.NotFound;

        var requestedIds = request.Variants.Select(v => v.Id).ToList();

        var variants = await dbContext.ProductVariants
            .Where(v => requestedIds.Contains(v.Id) && v.ProductId == productId && !v.IsDeleted)
            .ToListAsync(ct);

        if (variants.Count != requestedIds.Count)
            return ProductErrors.VariantNotFound;

        foreach (var item in request.Variants)
        {
            var variant = variants.First(v => v.Id == item.Id);
            variant.Name = item.Name;
            variant.Price = item.Price;
            variant.Quantity = item.Quantity;
            variant.IsActive = item.IsActive;

            if (item.Image is not null)
            {
                var upload = await storageService.UploadAsync(item.Image, "product-variants", ct);
                if (!upload.IsSuccess) return upload.Error;

                var oldPrimary = variant.Images.FirstOrDefault();
                var newImages = variant.Images.ToList();
                if (newImages.Count > 0)
                    newImages[0] = upload.Data;
                else
                    newImages.Add(upload.Data);
                variant.Images = newImages;

                if (oldPrimary is not null)
                    await TryDeleteFileAsync(oldPrimary);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateVariantAsync(long variantId, UpdateVariantRequest request,
        CancellationToken ct = default)
    {
        var variant = await dbContext.ProductVariants
            .SingleOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);

        if (variant is null) return ProductErrors.VariantNotFound;

        variant.Name = request.Name;
        variant.Price = request.Price;
        variant.Quantity = request.Quantity;
        variant.IsActive = request.IsActive;

        if (request.Image is not null)
        {
            var upload = await storageService.UploadAsync(request.Image, "product-variants", ct);
            if (!upload.IsSuccess) return upload.Error;

            var oldPrimary = variant.Images.FirstOrDefault();
            var newImages = variant.Images.ToList();
            if (newImages.Count > 0)
                newImages[0] = upload.Data;
            else
                newImages.Add(upload.Data);
            variant.Images = newImages;

            if (oldPrimary is not null)
                await TryDeleteFileAsync(oldPrimary);
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteVariantAsync(long variantId, CancellationToken ct = default)
    {
        var variant = await dbContext.ProductVariants
            .SingleOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);

        if (variant is null) return ProductErrors.VariantNotFound;

        variant.IsDeleted = true;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static List<List<VariantOption>> CartesianProduct(List<List<VariantOption>> sets)
    {
        var result = new List<List<VariantOption>> { new List<VariantOption>() };

        foreach (var set in sets)
        {
            result = result
                .SelectMany(existing => set.Select(opt => existing.Concat(new[] { opt }).ToList()))
                .ToList();
        }

        return result;
    }

    private static string BuildRedirectUrl(string name, long id, string baseUrl)
    {
        var slug = CreateProductSlug(name, id);

        return string.IsNullOrWhiteSpace(baseUrl)
            ? $"/product/{slug}"
            : $"{baseUrl}/product/{slug}";
    }

    private static string CreateProductSlug(string name, long id)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var chars = normalized
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        slug = slug.Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
            slug = "product";

        return $"{slug}-{id}";
    }

    // USD rate / category rule'lar mavjud bo'lmasa display narxlari raw qoladi (warning bilan)
    private async Task<PricingContext?> ResolvePricingContextAsync(string surface, CancellationToken ct)
    {
        var result = await pricingService.CreateContextAsync(ct);
        if (result.IsSuccess) return result.Data;

        logger.LogWarning(
            "Pricing context unavailable for {Surface}; returning raw prices ({Error})",
            surface, result.Error.Code);
        return null;
    }

    private async Task TryDeleteFileAsync(string fileUrl)
    {
        var result = await storageService.DeleteAsync(fileUrl);
        if (!result.IsSuccess)
            logger.LogWarning("Failed to delete file: {Url}", fileUrl);
    }
}