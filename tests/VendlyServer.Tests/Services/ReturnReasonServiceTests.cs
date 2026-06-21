using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.ReturnReasons;
using VendlyServer.Application.Services.ReturnReasons.Contracts;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class ReturnReasonServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReturnReasonService _service;

    public ReturnReasonServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ReturnReasonService(_db);
    }

    private static MultiLanguageField Ml(string value) => new()
    {
        Uz = value, Ru = value, En = value, Cyrl = value
    };

    private async Task<ReturnReason> SeedReason(string key, bool isDeleted = false)
    {
        var reason = new ReturnReason
        {
            Key = key,
            Name = Ml("name-" + key),
            Description = Ml("desc-" + key),
            CanResell = true,
            IsDeleted = isDeleted
        };
        _db.ReturnReasons.Add(reason);
        await _db.SaveChangesAsync();
        return reason;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsNonDeleted()
    {
        await SeedReason("damaged");
        await SeedReason("wrong_item");
        await SeedReason("old", isDeleted: true);

        var result = await _service.GetAllAsync(new ReturnReasonFilterRequest(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetAll_FiltersBySearch()
    {
        await SeedReason("damaged");
        await SeedReason("wrong_item");

        var result = await _service.GetAllAsync(new ReturnReasonFilterRequest("damaged"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsReason_WhenExists()
    {
        var reason = await SeedReason("damaged");

        var result = await _service.GetByIdAsync(reason.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("damaged", result.Data!.Key);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnReasonErrors.NotFound, result.Error);
    }

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_CreatesReason()
    {
        var result = await _service.AddAsync(
            new CreateReturnReasonRequest("damaged", Ml("Damaged"), Ml("Was damaged"), true));

        Assert.True(result.IsSuccess);
        Assert.Single(_db.ReturnReasons);
    }

    [Fact]
    public async Task Add_ReturnsKeyExists_WhenDuplicate()
    {
        await SeedReason("damaged");

        var result = await _service.AddAsync(
            new CreateReturnReasonRequest("damaged", Ml("Damaged"), Ml("x"), false));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnReasonErrors.KeyExists, result.Error);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesFields_WhenExists()
    {
        var reason = await SeedReason("damaged");

        var result = await _service.UpdateAsync(reason.Id,
            new CreateReturnReasonRequest("damaged_new", Ml("New"), Ml("New desc"), false));

        Assert.True(result.IsSuccess);
        var updated = _db.ReturnReasons.Single(r => r.Id == reason.Id);
        Assert.Equal("damaged_new", updated.Key);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.UpdateAsync(999,
            new CreateReturnReasonRequest("x", Ml("X"), Ml("Y"), false));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnReasonErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_ReturnsKeyExists_WhenKeyTakenByAnother()
    {
        await SeedReason("damaged");
        var target = await SeedReason("wrong_item");

        var result = await _service.UpdateAsync(target.Id,
            new CreateReturnReasonRequest("damaged", Ml("X"), Ml("Y"), false));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnReasonErrors.KeyExists, result.Error);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenExists()
    {
        var reason = await SeedReason("damaged");

        var result = await _service.DeleteAsync(reason.Id);

        Assert.True(result.IsSuccess);
        Assert.True(_db.ReturnReasons.Single(r => r.Id == reason.Id).IsDeleted);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnReasonErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
