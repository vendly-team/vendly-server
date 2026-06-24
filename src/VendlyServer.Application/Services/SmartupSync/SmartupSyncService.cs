using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Diagnostics;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Utils;
using VendlyServer.Infrastructure.Brokers.Smartup;
using VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;
using VendlyServer.Infrastructure.Extensions;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.SmartupSync;

public class SmartupSyncService(
    ISmartupBroker smartupBroker,
    AppDbContext dbContext,
    ILogger<SmartupSyncService> logger,
    IStorageService storageService) : ISmartupSyncService
{

    public async Task<Result> SyncAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString("N");
        var requestLogs = new List<SyncLog>();

        var log = new SyncLog
        {
            Source = "Smartup",
            Status = SyncStatus.Error,
            StartedAt = startedAt
        };

        dbContext.SyncLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Smartup Sync [{LogId}] [{CorrelationId}]: started", log.Id, correlationId);

        var categoriesCall = await smartupBroker.GetCategoriesAsync(cancellationToken);
        requestLogs.Add(CreateRequestLog(correlationId, categoriesCall));

        if (categoriesCall.Result.IsFailure)
        {
            logger.LogError("Smartup Sync [{LogId}]: failed to get categories — {Error}",
                log.Id, categoriesCall.Result.Error.Code);

            await FinishLogAsync(log, SyncStatus.Error, sw, correlationId, requestLogs,
                errorDetail: new { reason = categoriesCall.Result.Error.Code },
                cancellationToken: cancellationToken);

            return categoriesCall.Result.Error;
        }

        var categories = categoriesCall.Result.Data!
            .Where(c => !string.IsNullOrEmpty(c.ProductTypeId))
            .ToList();

        logger.LogInformation("Smartup Sync [{LogId}]: got {Count} categories", log.Id, categories.Count);

        var (categoryIdMap, catCreated, catUpdated) = await SyncCategoriesAsync(categories, log.Id, cancellationToken);

        var (prodCreated, prodUpdated, prodErrors, categoryDetails) =
            await SyncProductsAsync(categories, categoryIdMap, log.Id, requestLogs, correlationId, cancellationToken);

        sw.Stop();

        var status = prodErrors > 0 ? SyncStatus.Partial : SyncStatus.Success;

        log.Status = status;
        log.TotalProcessed = catCreated + catUpdated + prodCreated + prodUpdated;
        log.CreatedCount = catCreated + prodCreated;
        log.UpdatedCount = catUpdated + prodUpdated;
        log.ErrorCount = prodErrors;
        log.DurationMs = (int)sw.ElapsedMilliseconds;
        log.FinishedAt = DateTime.UtcNow;
        log.Response = JsonSerializer.SerializeToDocument(new
        {
            correlation_id = correlationId,
            categories = new { created = catCreated, updated = catUpdated },
            products = new { created = prodCreated, updated = prodUpdated, errors = prodErrors },
            details = categoryDetails
        });

        dbContext.SyncLogs.AddRange(requestLogs);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Smartup Sync [{LogId}]: {Status} in {Ms}ms — cat(+{CC}/~{CU}) prod(+{PC}/~{PU}/!{PE}) requests={Reqs}",
            log.Id, status, sw.ElapsedMilliseconds,
            catCreated, catUpdated, prodCreated, prodUpdated, prodErrors, requestLogs.Count);

        return Result.Success();
    }

    // ── Categories ────────────────────────────────────────────────────────

    private async Task<(Dictionary<string, long> idMap, int created, int updated)> SyncCategoriesAsync(
        List<SmartupCategoryItem> categories, long logId, CancellationToken cancellationToken)
    {
        var allDbCategories = await dbContext.Categories
            .Where(c => !c.IsDeleted && c.Metadata != null)
            .ToListAsync(cancellationToken);

        var existing = allDbCategories
            .Where(c => TryGetSourceId(c.Metadata, out _))
            .ToDictionary(c => GetSourceId(c.Metadata)!);

        var created = 0;
        var updated = 0;

        foreach (var cat in categories)
        {
            var sha = cat.Style?.L?.PhotoSha;
            var imageUrl = string.IsNullOrEmpty(sha) ? null : await ResolveImageUrlAsync(sha, cancellationToken);
            var metadata = JsonSerializer.SerializeToDocument(new { source_id = cat.ProductTypeId });

            var slug = SlugHelper.ToSlug(cat.Name);

            var localizedName = new MultiLanguageField
            {
                Cyrl = cat.Name.LatinToCyrillicUz(),
                Ru = cat.Name.LatinToCyrillicUz(),
                Uz = cat.Name.CyrillicToLatinUz(),
                En = cat.Name.CyrillicToLatinUz()
            };

            if (existing.TryGetValue(cat.ProductTypeId, out var entity))
            {
                entity.Name = localizedName;
                entity.Slug = slug;
                entity.Metadata = metadata;
                if (imageUrl is not null) entity.ImageUrl = imageUrl;
                updated++;
            }
            else
            {
                dbContext.Categories.Add(new Category
                {
                    Name = localizedName,
                    Slug = slug,
                    ImageUrl = imageUrl,
                    IsActive = true,
                    Metadata = metadata
                });
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Smartup Sync [{LogId}]: categories — +{Created} ~{Updated}",
            logId, created, updated);

        var idMap = await dbContext.Categories
            .Where(c => !c.IsDeleted && c.Metadata != null)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result
                .Where(c => TryGetSourceId(c.Metadata, out _))
                .ToDictionary(c => GetSourceId(c.Metadata)!, c => c.Id));

        return (idMap, created, updated);
    }

    // ── Products ──────────────────────────────────────────────────────────

    private const int MaxSyncPages = 500;

    private async Task<(int created, int updated, int errors, List<object> details)> SyncProductsAsync(
        List<SmartupCategoryItem> categories,
        Dictionary<string, long> categoryIdMap,
        long logId,
        List<SyncLog> requestLogs,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var totalCreated = 0;
        var totalUpdated = 0;
        var totalErrors = 0;
        var details = new List<object>();

        foreach (var cat in categories)
        {
            if (!categoryIdMap.TryGetValue(cat.ProductTypeId, out var categoryId))
                continue;

            var page = 1;
            var pageCount = 1;
            var catCreated = 0;
            var catUpdated = 0;
            var catErrors = 0;

            do
            {
                var call = await smartupBroker.GetProductsAsync(cat.ProductTypeId, page, cancellationToken);
                requestLogs.Add(CreateRequestLog(correlationId, call));

                if (call.Result.IsFailure)
                {
                    logger.LogWarning("Smartup Sync [{LogId}]: category {TypeId} page {Page} failed — {Error}",
                        logId, cat.ProductTypeId, page, call.Result.Error.Code);
                    catErrors++;
                    totalErrors++;
                    break;
                }

                var envelope = call.Result.Data!;

                if (page == 1)
                {
                    var rawPageCount = envelope.GetPageCount();
                    if (rawPageCount > MaxSyncPages)
                    {
                        logger.LogWarning(
                            "Smartup Sync [{LogId}]: category {TypeId} reports {Pages} pages — capping at {Max}",
                            logId, cat.ProductTypeId, rawPageCount, MaxSyncPages);
                        pageCount = MaxSyncPages;
                    }
                    else
                    {
                        pageCount = rawPageCount;
                    }
                }

                var (c, u) = await UpsertProductsAsync(envelope.Products, categoryId, cancellationToken);
                catCreated += c;
                catUpdated += u;
                totalCreated += c;
                totalUpdated += u;

                page++;
            }
            while (page <= pageCount);

            logger.LogInformation(
                "Smartup Sync [{LogId}]: [{CategoryName}] — +{Created} ~{Updated} !{Errors}",
                logId, cat.Name, catCreated, catUpdated, catErrors);

            details.Add(new
            {
                category = cat.Name,
                product_type_id = cat.ProductTypeId,
                created = catCreated,
                updated = catUpdated,
                errors = catErrors
            });
        }

        return (totalCreated, totalUpdated, totalErrors, details);
    }

    private async Task<(int created, int updated)> UpsertProductsAsync(
        List<SmartupProductItem> items, long categoryId, CancellationToken cancellationToken)
    {
        var dbProducts = await dbContext.Products
            .Where(p => !p.IsDeleted && p.CategoryId == categoryId && p.SyncSource == SyncSource.External)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Measurements)
            .ToListAsync(cancellationToken);

        var existingDict = dbProducts
            .Where(p => TryGetSourceId(p.Metadata, out _))
            .ToDictionary(p => GetSourceId(p.Metadata)!);

        var created = 0;
        var updated = 0;

        foreach (var item in items)
        {
            var price = decimal.TryParse(item.Price, out var p) ? p : 0m;
            var quantity = int.TryParse(item.BalanceQuant, out var q) ? q : 0;
            // weight_brutto, kg, konvertatsiyasiz. Parse bo'lmasa/null → 0 (default og'irlik QO'YMAYMIZ).
            var weightKg = decimal.TryParse(item.WeightBrutto, NumberStyles.Any, CultureInfo.InvariantCulture, out var w)
                ? w
                : 0m;
            var images = new List<string>();
            foreach (var sha in item.PhotoSha)
            {
                var url = await ResolveImageUrlAsync(sha, cancellationToken);
                if (url is not null) images.Add(url);
            }
            var metadata = JsonSerializer.SerializeToDocument(new
            {
                source_id = item.ProductId,
                code = item.Code,
                price_label = item.PriceLabel,
                measure = item.MeasureShortName
            });

            var trimmedName = TrimLastWords(item.Name, 2);
            var productName = new MultiLanguageField
            {
                Cyrl = trimmedName.LatinToCyrillicUz(),
                Ru = trimmedName.LatinToCyrillicUz(),
                Uz = trimmedName.CyrillicToLatinUz(),
                En = trimmedName.CyrillicToLatinUz()
            };

            if (existingDict.TryGetValue(item.ProductId, out var product))
            {
                product.Name = productName;
                product.Description = item.GenName;
                product.CategoryId = categoryId;
                product.IsActive = true;
                product.Metadata = metadata;

                var variant = product.Variants.FirstOrDefault(v => !v.IsDeleted);
                if (variant is not null)
                {
                    variant.Price = price;
                    variant.Quantity = quantity;
                    variant.Images = images;
                    variant.IsActive = true;

                    if (variant.Measurements is null)
                        variant.Measurements = new ProductMeasurement { WeightKg = weightKg };
                    else
                        variant.Measurements.WeightKg = weightKg;
                }

                updated++;
            }
            else
            {
                var newProduct = new Product
                {
                    Name = productName,
                    Description = item.GenName,
                    CategoryId = categoryId,
                    SyncSource = SyncSource.External,
                    IsActive = true,
                    Metadata = metadata
                };
                dbContext.Products.Add(newProduct);
                dbContext.ProductVariants.Add(new ProductVariant
                {
                    Product = newProduct,
                    Price = price,
                    Quantity = quantity,
                    Images = images,
                    IsActive = true,
                    Measurements = new ProductMeasurement { WeightKg = weightKg }
                });

                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static SyncLog CreateRequestLog<T>(string correlationId, SmartupCallResult<T> call)
    {
        return new SyncLog
        {
            Source = "Smartup:Request",
            Status = call.HttpSuccess ? SyncStatus.Success : SyncStatus.Error,
            StartedAt = call.StartedAt,
            FinishedAt = call.FinishedAt,
            DurationMs = call.DurationMs,
            RequestUrl = call.Url,
            RequestBody = call.RequestBody,
            Response = JsonSerializer.SerializeToDocument(new
            {
                correlation_id = correlationId,
                http_success = call.HttpSuccess,
                api_response = call.ResponseBody
            })
        };
    }

    private static string TrimLastWords(string name, int count)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= count ? name : string.Join(' ', parts[..^count]);
    }

    private static bool TryGetSourceId(JsonDocument? metadata, out string? sourceId)
    {
        sourceId = null;
        if (metadata is null) return false;
        if (!metadata.RootElement.TryGetProperty("source_id", out var prop)) return false;
        sourceId = prop.GetString();
        return sourceId is not null;
    }

    private static string? GetSourceId(JsonDocument? metadata)
    {
        TryGetSourceId(metadata, out var id);
        return id;
    }

    private async Task<string?> ResolveImageUrlAsync(string sha, CancellationToken cancellationToken)
    {
        var objectKey = $"smartup/{sha}";

        if (await storageService.ExistsAsync(objectKey, cancellationToken))
            return storageService.GetPublicUrl(objectKey);

        var download = await smartupBroker.DownloadImageAsync(sha, cancellationToken);
        if (download.IsFailure)
        {
            logger.LogWarning("Smartup: image sha={Sha} download failed — {Error}", sha, download.Error.Code);
            return null;
        }

        await using var content = download.Data!.Content;
        var upload = await storageService.UploadFromStreamAsync(
            content, objectKey, download.Data.ContentType, download.Data.Size, cancellationToken);

        if (upload.IsFailure)
        {
            logger.LogWarning("Smartup: image sha={Sha} upload failed — {Error}", sha, upload.Error.Code);
            return null;
        }

        return upload.Data;
    }

    private async Task FinishLogAsync(SyncLog log, SyncStatus status, Stopwatch sw,
        string correlationId, List<SyncLog> requestLogs,
        object? errorDetail = null, CancellationToken cancellationToken = default)
    {
        sw.Stop();
        log.Status = status;
        log.DurationMs = (int)sw.ElapsedMilliseconds;
        log.FinishedAt = DateTime.UtcNow;
        if (errorDetail is not null)
            log.ErrorDetail = JsonSerializer.SerializeToDocument(errorDetail);

        dbContext.SyncLogs.AddRange(requestLogs);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
