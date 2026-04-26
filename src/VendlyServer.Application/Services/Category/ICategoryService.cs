using VendlyServer.Application.Services.Category.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Category;

public interface ICategoryService
{
    Task<Result<List<CategoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
