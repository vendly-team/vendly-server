using VendlyServer.Application.Services.CategoryPrices.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.CategoryPrices;

public interface ICategoryPriceService
{
    Task<Result<List<CategoryPriceResponse>>> GetAllAsync(long? categoryId, CancellationToken cancellationToken = default);
    Task<Result<CategoryPriceResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<long>> AddAsync(CreateCategoryPriceRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, UpdateCategoryPriceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
