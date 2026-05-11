using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.SyncLogs;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Diagnostics;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class SyncLogServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SyncLogService _service;

    public SyncLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new SyncLogService(_db);

        _db.SyncLogs.AddRange(
            new SyncLog
            {
                Id = 1, Source = "smartup", Status = SyncStatus.Success,
                TotalProcessed = 100, CreatedCount = 80, UpdatedCount = 20,
                StartedAt = DateTime.UtcNow.AddHours(-2)
            },
            new SyncLog
            {
                Id = 2, Source = "manual", Status = SyncStatus.Error,
                TotalProcessed = 0, StartedAt = DateTime.UtcNow.AddHours(-1)
            },
            new SyncLog
            {
                Id = 3, Source = "deleted", Status = SyncStatus.Success,
                TotalProcessed = 5, StartedAt = DateTime.UtcNow, IsDeleted = true
            }
        );
        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ExcludesDeletedLogs()
    {
        var result = await _service.GetAllAsync(new DataQueryRequest { Page = 1, PageSize = 20 });

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, i => i.Source == "deleted");
    }

    [Fact]
    public async Task GetAll_ReturnsCorrectPage()
    {
        var result = await _service.GetAllAsync(new DataQueryRequest { Page = 1, PageSize = 1 });

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAll_ReturnsOrderedByStartedAtDescending()
    {
        var result = await _service.GetAllAsync(new DataQueryRequest { Page = 1, PageSize = 20 });

        Assert.True(result.Items[0].StartedAt >= result.Items[1].StartedAt);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsLog_WhenFound()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.Id);
        Assert.Equal("smartup", result.Data.Source);
        Assert.Equal(100, result.Data.TotalProcessed);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdDoesNotExist()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(SyncLogErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenLogIsDeleted()
    {
        var result = await _service.GetByIdAsync(3);

        Assert.False(result.IsSuccess);
        Assert.Equal(SyncLogErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
