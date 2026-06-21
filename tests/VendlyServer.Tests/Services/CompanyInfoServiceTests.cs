using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VendlyServer.Application.Services.CompanyInfo;
using VendlyServer.Application.Services.CompanyInfo.Contracts;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Persistence;
using CompanyInfoEntity = VendlyServer.Domain.Entities.Public.CompanyInfo;

namespace VendlyServer.Tests.Services;

public class CompanyInfoServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly StubStorageService _storage = new();
    private readonly CompanyInfoService _service;

    public CompanyInfoServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CompanyInfoService(_db, _storage, NullLogger<CompanyInfoService>.Instance);
    }

    private static UpsertCompanyInfoRequest Request(
        string? name = "Vendly",
        IFormFile? logo = null,
        IFormFile? ofertaUz = null) => new(
            Name: name, Phone: "+998", Email: "a@b.uz", Address: "Addr", WorkingHours: "9-18",
            Inn: "123", Mfo: "456", BankName: "Bank", AccountNumber: "789",
            Telegram: "tg", Instagram: "ig", Facebook: "fb", YouTube: "yt",
            BrandName: "Brand", Logo: logo,
            OfertaUz: ofertaUz, OfertaRu: null, OfertaEn: null, OfertaCyrl: null);

    private static IFormFile FakeFile(string name)
    {
        var bytes = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, name, name);
    }

    // ── GetAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsEmptyResponse_WhenNoneStored()
    {
        var result = await _service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data!.Name);
        Assert.NotNull(result.Data.OfertaUrl);
    }

    [Fact]
    public async Task Get_ReturnsStoredInfo()
    {
        _db.CompanyInfos.Add(new CompanyInfoEntity { Name = "Stored", Phone = "111" });
        await _db.SaveChangesAsync();

        var result = await _service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Stored", result.Data!.Name);
    }

    // ── UpsertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Upsert_CreatesRow_WhenNoneExists()
    {
        var result = await _service.UpsertAsync(Request(name: "New Co"));

        Assert.True(result.IsSuccess);
        Assert.Equal("New Co", result.Data!.Name);
        Assert.Single(_db.CompanyInfos);
    }

    [Fact]
    public async Task Upsert_UpdatesExistingRow_WithoutCreatingSecond()
    {
        _db.CompanyInfos.Add(new CompanyInfoEntity { Name = "Old" });
        await _db.SaveChangesAsync();

        var result = await _service.UpsertAsync(Request(name: "Updated"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", result.Data!.Name);
        Assert.Single(_db.CompanyInfos);
    }

    [Fact]
    public async Task Upsert_UploadsLogo_AndStoresUrl()
    {
        var result = await _service.UpsertAsync(Request(logo: FakeFile("logo.png")));

        Assert.True(result.IsSuccess);
        Assert.Equal("logo.png", result.Data!.LogoUrl);
    }

    [Fact]
    public async Task Upsert_UploadsOfertaUz_AndStoresUrl()
    {
        var result = await _service.UpsertAsync(Request(ofertaUz: FakeFile("oferta-uz.pdf")));

        Assert.True(result.IsSuccess);
        Assert.Equal("oferta-uz.pdf", result.Data!.OfertaUrl.Uz);
    }

    [Fact]
    public async Task Upsert_ReturnsError_WhenUploadFails()
    {
        _storage.FailUpload = true;

        var result = await _service.UpsertAsync(Request(logo: FakeFile("logo.png")));

        Assert.False(result.IsSuccess);
        Assert.Equal(StubStorageService.UploadError, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private class StubStorageService : IStorageService
    {
        public static readonly Error UploadError = Error.Failure("Storage.UploadFailed");
        public bool FailUpload { get; set; }

        public Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
            => Task.FromResult(FailUpload
                ? Result<string>.Failure(UploadError)
                : Result<string>.Success(file.FileName));

        public Task<Result> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result.Success());

        public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<Result<string>> UploadFromStreamAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default)
            => Task.FromResult(Result<string>.Success($"http://stub/{objectKey}"));

        public string GetPublicUrl(string objectKey) => $"http://stub/{objectKey}";
    }
}
