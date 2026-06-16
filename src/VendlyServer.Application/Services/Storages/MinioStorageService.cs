using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace VendlyServer.Application.Services.Storages;

public class MinioStorageService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    IOptions<StorageOptions> storageOptions) : IStorageService
{
    private readonly MinioOptions _minio = minioOptions.Value;
    private readonly StorageOptions _storage = storageOptions.Value;

    private static readonly Dictionary<string, string> MimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"]  = "image/png",
        [".webp"] = "image/webp",
        [".svg"]  = "image/svg+xml",
    };

    public async Task<Result<string>> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_storage.AllowedExtensions.Contains(extension))
            return StorageErrors.InvalidExtension;

        if (file.Length > _storage.MaxFileSizeBytes)
            return StorageErrors.FileTooLarge;

        var contentType = MimeMap.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var objectKey = $"{folder.Trim('/')}/{fileName}";

        try
        {
            await using var stream = file.OpenReadStream();

            var args = new PutObjectArgs()
                .WithBucket(_minio.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(contentType);

            await minioClient.PutObjectAsync(args, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StorageErrors.UploadFailed;
        }

        return $"{_minio.PublicBaseUrl.TrimEnd('/')}/{_minio.BucketName}/{objectKey}";
    }

    public async Task<Result> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var prefix = $"{_minio.PublicBaseUrl.TrimEnd('/')}/{_minio.BucketName}/";

        if (!fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return StorageErrors.InvalidUrl;

        var objectKey = fileUrl[prefix.Length..];

        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_minio.BucketName)
                .WithObject(objectKey);

            await minioClient.RemoveObjectAsync(args, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StorageErrors.DeleteFailed;
        }

        return Result.Success();
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_minio.BucketName)
                .WithObject(objectKey);

            await minioClient.StatObjectAsync(args, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<string>> UploadFromStreamAsync(
        Stream stream, string objectKey, string contentType, long size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new PutObjectArgs()
                .WithBucket(_minio.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(size)
                .WithContentType(contentType);

            await minioClient.PutObjectAsync(args, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StorageErrors.UploadFailed;
        }

        return GetPublicUrl(objectKey);
    }

    public string GetPublicUrl(string objectKey) =>
        $"{_minio.PublicBaseUrl.TrimEnd('/')}/{_minio.BucketName}/{objectKey}";
}
