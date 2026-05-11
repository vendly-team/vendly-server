using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VendlyServer.Application.Services.SmartupSync;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Brokers.Smartup;
using VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class SmartupSyncServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly StubSmartupBroker _broker;
    private readonly TrackingStorageService _storage;
    private readonly SmartupSyncService _service;

    public SmartupSyncServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(dbOptions);
        _broker = new StubSmartupBroker();
        _storage = new TrackingStorageService();

        _service = new SmartupSyncService(
            _broker,
            _db,
            NullLogger<SmartupSyncService>.Instance,
            _storage);
    }

    // ── Image resolution: already in MinIO ───────────────────────────────────

    [Fact]
    public async Task Sync_WhenImageAlreadyExistsInMinio_DoesNotDownload()
    {
        _broker.CategorySha = "abc123";
        _storage.ExistingKeys.Add("smartup/abc123");

        await _service.SyncAsync();

        Assert.Empty(_broker.DownloadedShas);
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "TestCategory");
        Assert.NotNull(cat);
        Assert.Equal("http://minio/vendly/smartup/abc123", cat!.ImageUrl);
    }

    // ── Image resolution: not in MinIO, download succeeds ────────────────────

    [Fact]
    public async Task Sync_WhenImageNotInMinio_DownloadsAndUploads()
    {
        _broker.CategorySha = "def456";

        await _service.SyncAsync();

        Assert.Contains("def456", _broker.DownloadedShas);
        Assert.Contains("smartup/def456", _storage.UploadedKeys);
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "TestCategory");
        Assert.Equal("http://minio/vendly/smartup/def456", cat!.ImageUrl);
    }

    // ── Image resolution: download fails, sync still completes ───────────────

    [Fact]
    public async Task Sync_WhenImageDownloadFails_SyncCompletesWithNullImageUrl()
    {
        _broker.CategorySha = "fail_sha";
        _broker.FailDownload = true;

        var result = await _service.SyncAsync();

        Assert.True(result.IsSuccess);
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Name == "TestCategory");
        Assert.Null(cat!.ImageUrl);
    }

    // ── Product images: multiple SHAs ─────────────────────────────────────────

    [Fact]
    public async Task Sync_ProductWithMultipleShas_UploadsAll()
    {
        _broker.ProductShas = ["sha1", "sha2"];

        await _service.SyncAsync();

        Assert.Contains("smartup/sha1", _storage.UploadedKeys);
        Assert.Contains("smartup/sha2", _storage.UploadedKeys);
        var variant = await _db.ProductVariants.FirstOrDefaultAsync();
        Assert.Equal(2, variant!.Images.Count);
    }

    public void Dispose() => _db.Dispose();

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private class StubSmartupBroker : ISmartupBroker
    {
        public string? CategorySha { get; set; }
        public List<string> ProductShas { get; set; } = [];
        public bool FailDownload { get; set; }
        public List<string> DownloadedShas { get; } = [];

        public Task<SmartupCallResult<List<SmartupCategoryItem>>> GetCategoriesAsync(CancellationToken ct = default)
        {
            var items = new List<SmartupCategoryItem>
            {
                new()
                {
                    ProductTypeId = "type1",
                    Name = "TestCategory",
                    Style = CategorySha is null ? null : new SmartupCategoryStyle
                    {
                        L = new SmartupCategoryStyleDetail { PhotoSha = CategorySha }
                    }
                }
            };

            var stub = new SmartupCallResult<List<SmartupCategoryItem>>(
                items,
                "http://stub",
                JsonDocument.Parse("{}"),
                null,
                0, true, DateTime.UtcNow, DateTime.UtcNow);

            return Task.FromResult(stub);
        }

        public Task<SmartupCallResult<SmartupProductsEnvelope>> GetProductsAsync(
            string productTypeId, int pageNo, CancellationToken ct = default)
        {
            var products = new List<SmartupProductItem>
            {
                new()
                {
                    ProductId = "prod1",
                    Name = "TestProduct",
                    Price = "100",
                    BalanceQuant = "5",
                    PhotoSha = ProductShas
                }
            };

            var envelope = new SmartupProductsEnvelope { Products = products, PageCount = "1" };
            var stub = new SmartupCallResult<SmartupProductsEnvelope>(
                envelope,
                "http://stub",
                JsonDocument.Parse("{}"),
                null,
                0, true, DateTime.UtcNow, DateTime.UtcNow);

            return Task.FromResult(stub);
        }

        public Task<Result<SmartupImageData>> DownloadImageAsync(string sha, CancellationToken ct = default)
        {
            DownloadedShas.Add(sha);

            if (FailDownload)
                return Task.FromResult(Result<SmartupImageData>.Failure(SmartupErrors.DownloadImageFailed));

            var stream = new MemoryStream([0x89, 0x50]);
            Result<SmartupImageData> result = new SmartupImageData(stream, "image/jpeg", stream.Length);
            return Task.FromResult(result);
        }
    }

    private class TrackingStorageService : IStorageService
    {
        public HashSet<string> ExistingKeys { get; } = [];
        public List<string> UploadedKeys { get; } = [];

        public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
            => Task.FromResult(ExistingKeys.Contains(objectKey));

        public Task<Result<string>> UploadFromStreamAsync(
            Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default)
        {
            UploadedKeys.Add(objectKey);
            return Task.FromResult(Result<string>.Success(GetPublicUrl(objectKey)));
        }

        public string GetPublicUrl(string objectKey) => $"http://minio/vendly/{objectKey}";

        public Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
            => Task.FromResult(Result<string>.Success($"http://stub/{folder}/{file.FileName}"));

        public Task<Result> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }
}
