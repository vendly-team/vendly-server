using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace VendlyServer.Application.Services.Storage;

public class MinioStorageService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    IOptions<StorageOptions> storageOptions) : IStorageService
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
        catch (MinioException)
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
        catch (MinioException)
        {
            return StorageErrors.DeleteFailed;
        }

        return Result.Success();
    }
}
