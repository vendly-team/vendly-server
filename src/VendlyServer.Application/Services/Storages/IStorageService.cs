using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Storages;

public interface IStorageService
{
    Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadFromStreamAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken cancellationToken = default);
    string GetPublicUrl(string objectKey);
}
