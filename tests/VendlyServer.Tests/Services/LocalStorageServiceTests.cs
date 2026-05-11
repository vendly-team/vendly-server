using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Storages;

namespace VendlyServer.Tests.Services;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _webRoot;
    private readonly LocalStorageService _service;
    private readonly StorageOptions _opts;

    public LocalStorageServiceTests()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_webRoot);

        _opts = new StorageOptions
        {
            BaseUrl = "/uploads",
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".svg"],
            MaxFileSizeBytes = 5_242_880
        };

        _service = new LocalStorageService(
            new StubWebHostEnvironment(_webRoot),
            Options.Create(_opts));
    }

    // ── GetPublicUrl ──────────────────────────────────────────────────────────

    [Fact]
    public void GetPublicUrl_ReturnsCorrectUrl()
    {
        var url = _service.GetPublicUrl("categories/img.jpg");

        Assert.Equal("/uploads/uploads/categories/img.jpg", url);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenFileNotPresent()
    {
        var result = await _service.ExistsAsync("categories/nonexistent.jpg");

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenFilePresent()
    {
        var dir = Path.Combine(_webRoot, "uploads", "categories");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "test.jpg");
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        var result = await _service.ExistsAsync("categories/test.jpg");

        Assert.True(result);
    }

    // ── UploadAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ReturnsInvalidExtension_WhenExtensionNotAllowed()
    {
        var file = MakeFormFile("malware.exe", 100);

        var result = await _service.UploadAsync(file, "categories");

        Assert.False(result.IsSuccess);
        Assert.Equal(StorageErrors.InvalidExtension, result.Error);
    }

    [Fact]
    public async Task Upload_ReturnsFileTooLarge_WhenFileSizeExceedsLimit()
    {
        var file = MakeFormFile("photo.jpg", _opts.MaxFileSizeBytes + 1);

        var result = await _service.UploadAsync(file, "categories");

        Assert.False(result.IsSuccess);
        Assert.Equal(StorageErrors.FileTooLarge, result.Error);
    }

    [Fact]
    public async Task Upload_SavesFileAndReturnsUrl_WhenValid()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads", "categories"));
        var file = MakeFormFile("photo.jpg", 512);

        var result = await _service.UploadAsync(file, "categories");

        Assert.True(result.IsSuccess);
        Assert.Contains("categories/", result.Data);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsSuccess_WhenFileDoesNotExist()
    {
        var result = await _service.DeleteAsync("nonexistent/path.jpg");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Delete_DeletesFile_WhenFileExists()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads"));
        var filePath = Path.Combine(_webRoot, "uploads", "to-delete.jpg");
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        var result = await _service.DeleteAsync("uploads/to-delete.jpg");

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(filePath));
    }

    // ── UploadFromStreamAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UploadFromStream_SavesFileAndReturnsUrl()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads", "products"));
        using var stream = new MemoryStream([10, 20, 30]);

        var result = await _service.UploadFromStreamAsync(stream, "products/item.jpg", "image/jpeg", 3);

        Assert.True(result.IsSuccess);
        Assert.Contains("products/item.jpg", result.Data);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
            Directory.Delete(_webRoot, recursive: true);
    }

    private static IFormFile MakeFormFile(string fileName, long size)
    {
        var bytes = new byte[Math.Min(size, 4096)];
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, size, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
        return file;
    }

    private sealed class StubWebHostEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
    }
}
