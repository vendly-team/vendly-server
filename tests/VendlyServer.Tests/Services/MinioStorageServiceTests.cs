using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using VendlyServer.Application.Services.Storages;

namespace VendlyServer.Tests.Services;

public class MinioStorageServiceTests
{
    private readonly StorageOptions _storageOpts = new()
    {
        AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".svg"],
        MaxFileSizeBytes = 5_242_880
    };

    private readonly MinioOptions _minioOpts = new()
    {
        BucketName = "vendly",
        PublicBaseUrl = "https://cdn.example.com"
    };

    private MinioStorageService BuildService() =>
        new(null!, Options.Create(_minioOpts), Options.Create(_storageOpts));

    // ── GetPublicUrl ──────────────────────────────────────────────────────────

    [Fact]
    public void GetPublicUrl_ReturnsCorrectUrl()
    {
        var service = BuildService();

        var url = service.GetPublicUrl("categories/img.jpg");

        Assert.Equal("https://cdn.example.com/vendly/categories/img.jpg", url);
    }

    // ── UploadAsync — validation (no MinioClient needed) ─────────────────────

    [Fact]
    public async Task Upload_ReturnsInvalidExtension_WhenExtensionNotAllowed()
    {
        var service = BuildService();
        var file = MakeFormFile("malware.exe", 100);

        var result = await service.UploadAsync(file, "products");

        Assert.False(result.IsSuccess);
        Assert.Equal(StorageErrors.InvalidExtension, result.Error);
    }

    [Fact]
    public async Task Upload_ReturnsFileTooLarge_WhenSizeExceedsLimit()
    {
        var service = BuildService();
        var file = MakeFormFile("img.jpg", _storageOpts.MaxFileSizeBytes + 1);

        var result = await service.UploadAsync(file, "products");

        Assert.False(result.IsSuccess);
        Assert.Equal(StorageErrors.FileTooLarge, result.Error);
    }

    // ── DeleteAsync — validation (no MinioClient needed) ─────────────────────

    [Fact]
    public async Task Delete_ReturnsInvalidUrl_WhenUrlDoesNotMatchBucket()
    {
        var service = BuildService();

        var result = await service.DeleteAsync("https://other.com/someobj");

        Assert.False(result.IsSuccess);
        Assert.Equal(StorageErrors.InvalidUrl, result.Error);
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
}
