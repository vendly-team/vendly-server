using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Storages;

public class LocalStorageService(
    IWebHostEnvironment environment,
    IOptions<StorageOptions> options) : IStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
            return StorageErrors.InvalidExtension;

        if (file.Length > _options.MaxFileSizeBytes)
            return StorageErrors.FileTooLarge;

        var fileName   = $"{Guid.NewGuid()}{extension}";
        var folderPath = Path.Combine(environment.WebRootPath, "uploads", folder);

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return $"{_options.BaseUrl}/{folder}/{fileName}";
    }

    public Task<Result> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath     = Path.Combine(environment.WebRootPath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.FromResult(Result.Success());
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = TryGetSafePath(objectKey);
        return Task.FromResult(fullPath is not null && File.Exists(fullPath));
    }

    public async Task<Result<string>> UploadFromStreamAsync(
        Stream stream, string objectKey, string contentType, long size,
        CancellationToken cancellationToken = default)
    {
        var fullPath = TryGetSafePath(objectKey);
        if (fullPath is null)
            return StorageErrors.InvalidKey;

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs, cancellationToken);

        return GetPublicUrl(objectKey);
    }

    public string GetPublicUrl(string objectKey) =>
        $"{_options.BaseUrl}/uploads/{objectKey}";

    private string? TryGetSafePath(string objectKey)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        var fullPath    = Path.GetFullPath(Path.Combine(uploadsRoot, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
    }
}
