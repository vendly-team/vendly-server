using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.Storage;

public interface IStorageService
{
    Task<Result<string>> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
