using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace VendlyServer.Application.Services.Storages;

public class MinioStorageService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    IOptions<StorageOptions> storageOptions,
    ILogger<MinioStorageService> logger) : IStorageService
{
    private readonly MinioOptions _minio = minioOptions.Value;
    private readonly StorageOptions _storage = storageOptions.Value;

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
                .WithContentType(file.ContentType ?? "application/octet-stream");

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MinIO StatObject for {ObjectKey} threw an unexpected exception; treating as non-existent", objectKey);
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
