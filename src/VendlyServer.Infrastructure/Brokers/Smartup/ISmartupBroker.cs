using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public interface ISmartupBroker
{
    Task<SmartupCallResult<List<SmartupCategoryItem>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<SmartupCallResult<SmartupProductsEnvelope>> GetProductsAsync(string productTypeId, int pageNo, CancellationToken cancellationToken = default);
    Task<Result<SmartupImageData>> DownloadImageAsync(string sha, CancellationToken cancellationToken = default);
}
