using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.BtsRef;

public class BtsRefService(AppDbContext dbContext) : IBtsRefService
{
    // ── Regions ──────────────────────────────────────────────────────────────

    public async Task<Result<List<BtsRegionResponse>>> GetAllRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BtsRegions
            .AsNoTracking()
            .Select(r => new BtsRegionResponse { Id = r.Id, Code = r.Code, Name = r.Name, SyncedAt = r.SyncedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BtsRegionResponse>> GetRegionByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var region = await dbContext.BtsRegions
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new BtsRegionResponse { Id = r.Id, Code = r.Code, Name = r.Name, SyncedAt = r.SyncedAt })
            .SingleOrDefaultAsync(cancellationToken);

        return region is null ? BtsRefErrors.RegionNotFound : region;
    }

    public async Task<Result> AddRegionAsync(SaveBtsRegionRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.BtsRegions.AsNoTracking()
            .AnyAsync(r => r.Code == request.Code, cancellationToken);
        if (exists) return BtsRefErrors.RegionCodeExists;

        dbContext.BtsRegions.Add(new BtsRegionRef
        {
            Code = request.Code,
            Name = request.Name,
            SyncedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateRegionAsync(long id, SaveBtsRegionRequest request, CancellationToken cancellationToken = default)
    {
        var region = await dbContext.BtsRegions
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (region is null) return BtsRefErrors.RegionNotFound;

        var codeExists = await dbContext.BtsRegions.AsNoTracking()
            .AnyAsync(r => r.Code == request.Code && r.Id != id, cancellationToken);
        if (codeExists) return BtsRefErrors.RegionCodeExists;

        region.Code = request.Code;
        region.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteRegionAsync(long id, CancellationToken cancellationToken = default)
    {
        var region = await dbContext.BtsRegions
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (region is null) return BtsRefErrors.RegionNotFound;

        dbContext.BtsRegions.Remove(region);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ── Cities ───────────────────────────────────────────────────────────────

    public async Task<Result<List<BtsCityResponse>>> GetAllCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BtsCities
            .AsNoTracking()
            .Select(c => new BtsCityResponse { Id = c.Id, RegionCode = c.RegionCode, Code = c.Code, Name = c.Name, SyncedAt = c.SyncedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BtsCityResponse>> GetCityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var city = await dbContext.BtsCities
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new BtsCityResponse { Id = c.Id, RegionCode = c.RegionCode, Code = c.Code, Name = c.Name, SyncedAt = c.SyncedAt })
            .SingleOrDefaultAsync(cancellationToken);

        return city is null ? BtsRefErrors.CityNotFound : city;
    }

    public async Task<Result> AddCityAsync(SaveBtsCityRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.BtsCities.AsNoTracking()
            .AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (exists) return BtsRefErrors.CityCodeExists;

        dbContext.BtsCities.Add(new BtsCityRef
        {
            RegionCode = request.RegionCode,
            Code = request.Code,
            Name = request.Name,
            SyncedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateCityAsync(long id, SaveBtsCityRequest request, CancellationToken cancellationToken = default)
    {
        var city = await dbContext.BtsCities
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (city is null) return BtsRefErrors.CityNotFound;

        var codeExists = await dbContext.BtsCities.AsNoTracking()
            .AnyAsync(c => c.Code == request.Code && c.Id != id, cancellationToken);
        if (codeExists) return BtsRefErrors.CityCodeExists;

        city.RegionCode = request.RegionCode;
        city.Code = request.Code;
        city.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteCityAsync(long id, CancellationToken cancellationToken = default)
    {
        var city = await dbContext.BtsCities
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (city is null) return BtsRefErrors.CityNotFound;

        dbContext.BtsCities.Remove(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ── Branches ─────────────────────────────────────────────────────────────

    public async Task<Result<List<BtsBranchResponse>>> GetAllBranchesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BtsBranches
            .AsNoTracking()
            .Select(b => new BtsBranchResponse
            {
                Id = b.Id, RegionCode = b.RegionCode, CityCode = b.CityCode,
                Code = b.Code, Name = b.Name, Address = b.Address,
                Phone = b.Phone, LatLong = b.LatLong, WorkingHours = b.WorkingHours, SyncedAt = b.SyncedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BtsBranchResponse>> GetBranchByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.BtsBranches
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BtsBranchResponse
            {
                Id = b.Id, RegionCode = b.RegionCode, CityCode = b.CityCode,
                Code = b.Code, Name = b.Name, Address = b.Address,
                Phone = b.Phone, LatLong = b.LatLong, WorkingHours = b.WorkingHours, SyncedAt = b.SyncedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return branch is null ? BtsRefErrors.BranchNotFound : branch;
    }

    public async Task<Result> AddBranchAsync(SaveBtsBranchRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.BtsBranches.AsNoTracking()
            .AnyAsync(b => b.Code == request.Code, cancellationToken);
        if (exists) return BtsRefErrors.BranchCodeExists;

        dbContext.BtsBranches.Add(new BtsBranchRef
        {
            RegionCode = request.RegionCode,
            CityCode = request.CityCode,
            Code = request.Code,
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            LatLong = request.LatLong,
            SyncedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateBranchAsync(long id, SaveBtsBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.BtsBranches
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (branch is null) return BtsRefErrors.BranchNotFound;

        var codeExists = await dbContext.BtsBranches.AsNoTracking()
            .AnyAsync(b => b.Code == request.Code && b.Id != id, cancellationToken);
        if (codeExists) return BtsRefErrors.BranchCodeExists;

        branch.RegionCode = request.RegionCode;
        branch.CityCode = request.CityCode;
        branch.Code = request.Code;
        branch.Name = request.Name;
        branch.Address = request.Address;
        branch.Phone = request.Phone;
        branch.LatLong = request.LatLong;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteBranchAsync(long id, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.BtsBranches
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (branch is null) return BtsRefErrors.BranchNotFound;

        dbContext.BtsBranches.Remove(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ── Package Types ─────────────────────────────────────────────────────────

    public async Task<Result<List<BtsPackageTypeResponse>>> GetAllPackageTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BtsPackageTypes
            .AsNoTracking()
            .Select(p => new BtsPackageTypeResponse { Id = p.Id, BtsId = p.BtsId, Name = p.Name, SyncedAt = p.SyncedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BtsPackageTypeResponse>> GetPackageTypeByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPackageTypes
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new BtsPackageTypeResponse { Id = p.Id, BtsId = p.BtsId, Name = p.Name, SyncedAt = p.SyncedAt })
            .SingleOrDefaultAsync(cancellationToken);

        return pt is null ? BtsRefErrors.PackageTypeNotFound : pt;
    }

    public async Task<Result> AddPackageTypeAsync(SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.BtsPackageTypes.AsNoTracking()
            .AnyAsync(p => p.BtsId == request.BtsId, cancellationToken);
        if (exists) return BtsRefErrors.PackageTypeBtsIdExists;

        dbContext.BtsPackageTypes.Add(new BtsPackageTypeRef
        {
            BtsId = request.BtsId,
            Name = request.Name,
            SyncedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdatePackageTypeAsync(long id, SaveBtsPackageTypeRequest request, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPackageTypes
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pt is null) return BtsRefErrors.PackageTypeNotFound;

        var btsIdExists = await dbContext.BtsPackageTypes.AsNoTracking()
            .AnyAsync(p => p.BtsId == request.BtsId && p.Id != id, cancellationToken);
        if (btsIdExists) return BtsRefErrors.PackageTypeBtsIdExists;

        pt.BtsId = request.BtsId;
        pt.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeletePackageTypeAsync(long id, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPackageTypes
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pt is null) return BtsRefErrors.PackageTypeNotFound;

        dbContext.BtsPackageTypes.Remove(pt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ── Post Types ────────────────────────────────────────────────────────────

    public async Task<Result<List<BtsPostTypeResponse>>> GetAllPostTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.BtsPostTypes
            .AsNoTracking()
            .Select(p => new BtsPostTypeResponse { Id = p.Id, BtsId = p.BtsId, Name = p.Name, SyncedAt = p.SyncedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BtsPostTypeResponse>> GetPostTypeByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPostTypes
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new BtsPostTypeResponse { Id = p.Id, BtsId = p.BtsId, Name = p.Name, SyncedAt = p.SyncedAt })
            .SingleOrDefaultAsync(cancellationToken);

        return pt is null ? BtsRefErrors.PostTypeNotFound : pt;
    }

    public async Task<Result> AddPostTypeAsync(SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.BtsPostTypes.AsNoTracking()
            .AnyAsync(p => p.BtsId == request.BtsId, cancellationToken);
        if (exists) return BtsRefErrors.PostTypeBtsIdExists;

        dbContext.BtsPostTypes.Add(new BtsPostTypeRef
        {
            BtsId = request.BtsId,
            Name = request.Name,
            SyncedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdatePostTypeAsync(long id, SaveBtsPostTypeRequest request, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPostTypes
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pt is null) return BtsRefErrors.PostTypeNotFound;

        var btsIdExists = await dbContext.BtsPostTypes.AsNoTracking()
            .AnyAsync(p => p.BtsId == request.BtsId && p.Id != id, cancellationToken);
        if (btsIdExists) return BtsRefErrors.PostTypeBtsIdExists;

        pt.BtsId = request.BtsId;
        pt.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeletePostTypeAsync(long id, CancellationToken cancellationToken = default)
    {
        var pt = await dbContext.BtsPostTypes
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pt is null) return BtsRefErrors.PostTypeNotFound;

        dbContext.BtsPostTypes.Remove(pt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
