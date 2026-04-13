using Microsoft.Extensions.Logging;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Jobs.BtsCatalog;

public class BtsCatalogSyncJob(
    IBtsBroker btsBroker,
    AppDbContext dbContext,
    ILogger<BtsCatalogSyncJob> logger) : IBtsCatalogSyncJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("BTS Catalog Sync: started at {StartedAt}", DateTime.UtcNow);

        var syncedAt = DateTime.UtcNow;

        try
        {
            var regionsResult = await SyncRegionsAsync(syncedAt, cancellationToken);
            if (regionsResult.IsFailure)
            {
                logger.LogError("BTS Catalog Sync: aborted — failed to sync regions: {Error}", regionsResult.Error.Code);
                return;
            }

            var regions = regionsResult.Data!;
            await SyncCitiesAndBranchesAsync(regions, syncedAt, cancellationToken);
            await SyncPackageTypesAsync(syncedAt, cancellationToken);
            await SyncPostTypesAsync(syncedAt, cancellationToken);

            logger.LogInformation("BTS Catalog Sync: completed successfully at {FinishedAt}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BTS Catalog Sync: failed with unexpected exception");
            throw;
        }
    }

    private async Task<Result<List<BtsRegion>>> SyncRegionsAsync(DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        var result = await btsBroker.GetRegionsAsync(forceRefresh: true, cancellationToken);
        if (result.IsFailure) return result.Error;

        var regions = result.Data!;
        var existing = await dbContext.BtsRegions
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var createdCount = 0;
        var updatedCount = 0;

        foreach (var region in regions)
        {
            if (existing.TryGetValue(region.Code, out var entity))
            {
                entity.Name = region.Name;
                entity.SyncedAt = syncedAt;
                updatedCount++;
            }
            else
            {
                dbContext.BtsRegions.Add(new BtsRegionRef
                {
                    Code = region.Code,
                    Name = region.Name,
                    SyncedAt = syncedAt
                });
                createdCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("BTS Catalog Sync: regions — created {Created}, updated {Updated}",
            createdCount, updatedCount);

        return regions;
    }

    private async Task SyncCitiesAndBranchesAsync(List<BtsRegion> regions, DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        var existingCities = await dbContext.BtsCities
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var existingBranches = await dbContext.BtsBranches
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var citiesCreated = 0;
        var citiesUpdated = 0;
        var branchesCreated = 0;
        var branchesUpdated = 0;

        foreach (var region in regions)
        {
            var citiesResult = await btsBroker.GetCitiesAsync(region.Code, forceRefresh: true, cancellationToken);
            if (citiesResult.IsFailure)
            {
                logger.LogWarning("BTS Catalog Sync: cities for region {RegionCode} failed: {Error}",
                    region.Code, citiesResult.Error.Code);
                continue;
            }

            var cities = citiesResult.Data!;
            foreach (var city in cities)
            {
                if (existingCities.TryGetValue(city.Code, out var cityEntity))
                {
                    cityEntity.RegionCode = region.Code;
                    cityEntity.Name = city.Name;
                    cityEntity.SyncedAt = syncedAt;
                    citiesUpdated++;
                }
                else
                {
                    dbContext.BtsCities.Add(new BtsCityRef
                    {
                        RegionCode = region.Code,
                        Code = city.Code,
                        Name = city.Name,
                        SyncedAt = syncedAt
                    });
                    citiesCreated++;
                }

                var branchesResult = await btsBroker.GetBranchesAsync(
                    region.Code, city.Code, forceRefresh: true, cancellationToken);

                if (branchesResult.IsFailure)
                {
                    logger.LogWarning(
                        "BTS Catalog Sync: branches for region {RegionCode} city {CityCode} failed: {Error}",
                        region.Code, city.Code, branchesResult.Error.Code);
                    continue;
                }

                foreach (var branch in branchesResult.Data!)
                {
                    if (existingBranches.TryGetValue(branch.Code, out var branchEntity))
                    {
                        branchEntity.RegionCode = branch.RegionCode;
                        branchEntity.CityCode = branch.CityCode;
                        branchEntity.Name = branch.Name;
                        branchEntity.Address = branch.Address;
                        branchEntity.Phone = branch.Phone;
                        branchEntity.LatLong = branch.LatLong;
                        branchEntity.WorkingHours = SerializeWorkingHours(branch.WorkingHours);
                        branchEntity.SyncedAt = syncedAt;
                        branchesUpdated++;
                    }
                    else
                    {
                        dbContext.BtsBranches.Add(new BtsBranchRef
                        {
                            RegionCode = branch.RegionCode,
                            CityCode = branch.CityCode,
                            Code = branch.Code,
                            Name = branch.Name,
                            Address = branch.Address,
                            Phone = branch.Phone,
                            LatLong = branch.LatLong,
                            WorkingHours = SerializeWorkingHours(branch.WorkingHours),
                            SyncedAt = syncedAt
                        });
                        branchesCreated++;
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "BTS Catalog Sync: cities — created {CitiesCreated}, updated {CitiesUpdated}; " +
            "branches — created {BranchesCreated}, updated {BranchesUpdated}",
            citiesCreated, citiesUpdated, branchesCreated, branchesUpdated);
    }

    private async Task SyncPackageTypesAsync(DateTime syncedAt, CancellationToken cancellationToken)
    {
        var result = await btsBroker.GetPackageTypesAsync(forceRefresh: true, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("BTS Catalog Sync: package types failed: {Error}", result.Error.Code);
            return;
        }

        var existing = await dbContext.BtsPackageTypes
            .ToDictionaryAsync(x => x.BtsId, cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var package in result.Data!)
        {
            if (existing.TryGetValue(package.Id, out var entity))
            {
                entity.Name = package.Name;
                entity.SyncedAt = syncedAt;
                updated++;
            }
            else
            {
                dbContext.BtsPackageTypes.Add(new BtsPackageTypeRef
                {
                    BtsId = package.Id,
                    Name = package.Name,
                    SyncedAt = syncedAt
                });
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("BTS Catalog Sync: package types — created {Created}, updated {Updated}",
            created, updated);
    }

    private async Task SyncPostTypesAsync(DateTime syncedAt, CancellationToken cancellationToken)
    {
        var result = await btsBroker.GetPostTypesAsync(forceRefresh: true, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("BTS Catalog Sync: post types failed: {Error}", result.Error.Code);
            return;
        }

        var existing = await dbContext.BtsPostTypes
            .ToDictionaryAsync(x => x.BtsId, cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var postType in result.Data!)
        {
            if (existing.TryGetValue(postType.Id, out var entity))
            {
                entity.Name = postType.Name;
                entity.SyncedAt = syncedAt;
                updated++;
            }
            else
            {
                dbContext.BtsPostTypes.Add(new BtsPostTypeRef
                {
                    BtsId = postType.Id,
                    Name = postType.Name,
                    SyncedAt = syncedAt
                });
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("BTS Catalog Sync: post types — created {Created}, updated {Updated}",
            created, updated);
    }

    private static System.Text.Json.JsonDocument? SerializeWorkingHours(
        Dictionary<string, string?>? workingHours)
    {
        if (workingHours is null || workingHours.Count == 0)
            return null;

        return System.Text.Json.JsonSerializer.SerializeToDocument(workingHours);
    }
}
