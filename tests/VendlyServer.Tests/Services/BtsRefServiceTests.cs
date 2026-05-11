using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.BtsRef.Contracts;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class BtsRefServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BtsRefService _service;

    public BtsRefServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new BtsRefService(_db);

        _db.BtsRegions.AddRange(
            new BtsRegionRef { Id = 1, Code = "R01", Name = "Toshkent", SyncedAt = DateTime.UtcNow },
            new BtsRegionRef { Id = 2, Code = "R02", Name = "Andijon", SyncedAt = DateTime.UtcNow }
        );
        _db.BtsCities.AddRange(
            new BtsCityRef { Id = 1, RegionCode = "R01", Code = "C01", Name = "Toshkent sh.", SyncedAt = DateTime.UtcNow },
            new BtsCityRef { Id = 2, RegionCode = "R01", Code = "C02", Name = "Chirchiq", SyncedAt = DateTime.UtcNow }
        );
        _db.BtsBranches.Add(new BtsBranchRef
        {
            Id = 1, RegionCode = "R01", CityCode = "C01", Code = "B01",
            Name = "Filial 1", Address = "Ko'cha 1", SyncedAt = DateTime.UtcNow
        });
        _db.BtsPackageTypes.Add(new BtsPackageTypeRef { Id = 1, BtsId = 10, Name = "Standard", SyncedAt = DateTime.UtcNow });
        _db.BtsPostTypes.Add(new BtsPostTypeRef { Id = 1, BtsId = 20, Name = "Express", SyncedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    // ── Regions ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRegions_ReturnsAllRegions()
    {
        var result = await _service.GetAllRegionsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetRegionById_ReturnsRegion_WhenFound()
    {
        var result = await _service.GetRegionByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("R01", result.Data!.Code);
        Assert.Equal("Toshkent", result.Data.Name);
    }

    [Fact]
    public async Task GetRegionById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetRegionByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.RegionNotFound, result.Error);
    }

    [Fact]
    public async Task AddRegion_CreatesRegion_WhenCodeIsUnique()
    {
        var result = await _service.AddRegionAsync(new SaveBtsRegionRequest { Code = "R99", Name = "Namangan" });

        Assert.True(result.IsSuccess);
        Assert.True(await _db.BtsRegions.AnyAsync(r => r.Code == "R99"));
    }

    [Fact]
    public async Task AddRegion_ReturnsCodeExists_WhenDuplicate()
    {
        var result = await _service.AddRegionAsync(new SaveBtsRegionRequest { Code = "R01", Name = "Duplicate" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.RegionCodeExists, result.Error);
    }

    [Fact]
    public async Task UpdateRegion_UpdatesFields_WhenFound()
    {
        var result = await _service.UpdateRegionAsync(1, new SaveBtsRegionRequest { Code = "R01", Name = "Updated" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", _db.BtsRegions.Single(r => r.Id == 1).Name);
    }

    [Fact]
    public async Task UpdateRegion_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.UpdateRegionAsync(999, new SaveBtsRegionRequest { Code = "RXX", Name = "X" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.RegionNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateRegion_ReturnsCodeExists_WhenCodeConflictsWithOther()
    {
        var result = await _service.UpdateRegionAsync(1, new SaveBtsRegionRequest { Code = "R02", Name = "Conflict" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.RegionCodeExists, result.Error);
    }

    [Fact]
    public async Task DeleteRegion_RemovesRegion_WhenFound()
    {
        var result = await _service.DeleteRegionAsync(2);

        Assert.True(result.IsSuccess);
        Assert.False(await _db.BtsRegions.AnyAsync(r => r.Id == 2));
    }

    [Fact]
    public async Task DeleteRegion_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteRegionAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.RegionNotFound, result.Error);
    }

    // ── Cities ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCities_ReturnsAllCities()
    {
        var result = await _service.GetAllCitiesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetCitiesByRegion_ReturnsFilteredCities()
    {
        var result = await _service.GetCitiesByRegionAsync("R01");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data, c => Assert.Equal("R01", c.RegionCode));
    }

    [Fact]
    public async Task GetCityById_ReturnsCityNotFound_WhenMissing()
    {
        var result = await _service.GetCityByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.CityNotFound, result.Error);
    }

    [Fact]
    public async Task AddCity_CreatesCity_WhenCodeIsUnique()
    {
        var result = await _service.AddCityAsync(new SaveBtsCityRequest { RegionCode = "R02", Code = "C99", Name = "Andijon sh." });

        Assert.True(result.IsSuccess);
        Assert.True(await _db.BtsCities.AnyAsync(c => c.Code == "C99"));
    }

    [Fact]
    public async Task AddCity_ReturnsCityCodeExists_WhenDuplicate()
    {
        var result = await _service.AddCityAsync(new SaveBtsCityRequest { RegionCode = "R01", Code = "C01", Name = "Dup" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.CityCodeExists, result.Error);
    }

    [Fact]
    public async Task DeleteCity_RemovesCity_WhenFound()
    {
        var result = await _service.DeleteCityAsync(2);

        Assert.True(result.IsSuccess);
        Assert.False(await _db.BtsCities.AnyAsync(c => c.Id == 2));
    }

    // ── Branches ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllBranches_ReturnsBranches()
    {
        var result = await _service.GetAllBranchesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetBranchById_ReturnsBranchNotFound_WhenMissing()
    {
        var result = await _service.GetBranchByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task AddBranch_CreatesBranch_WhenCodeIsUnique()
    {
        var result = await _service.AddBranchAsync(new SaveBtsBranchRequest
        {
            RegionCode = "R01", CityCode = "C02", Code = "B99",
            Name = "Filial 99", Address = "Ko'cha 99"
        });

        Assert.True(result.IsSuccess);
        Assert.True(await _db.BtsBranches.AnyAsync(b => b.Code == "B99"));
    }

    [Fact]
    public async Task DeleteBranch_ReturnsBranchNotFound_WhenMissing()
    {
        var result = await _service.DeleteBranchAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.BranchNotFound, result.Error);
    }

    // ── Package Types ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPackageTypes_ReturnsAll()
    {
        var result = await _service.GetAllPackageTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task AddPackageType_ReturnsConflict_WhenBtsIdExists()
    {
        var result = await _service.AddPackageTypeAsync(new SaveBtsPackageTypeRequest { BtsId = 10, Name = "Dup" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.PackageTypeBtsIdExists, result.Error);
    }

    [Fact]
    public async Task DeletePackageType_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeletePackageTypeAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.PackageTypeNotFound, result.Error);
    }

    // ── Post Types ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPostTypes_ReturnsAll()
    {
        var result = await _service.GetAllPostTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task AddPostType_ReturnsConflict_WhenBtsIdExists()
    {
        var result = await _service.AddPostTypeAsync(new SaveBtsPostTypeRequest { BtsId = 20, Name = "Dup" });

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.PostTypeBtsIdExists, result.Error);
    }

    [Fact]
    public async Task DeletePostType_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeletePostTypeAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(BtsRefErrors.PostTypeNotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
