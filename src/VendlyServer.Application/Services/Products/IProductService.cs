using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Products;

public interface IProductService
{
    Task<Result<List<ProductListResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<ProductAdminDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(long id, CancellationToken ct = default);
    Task<Result> ToggleActiveAsync(long id, CancellationToken ct = default);

    Task<Result> AddVariantTypeAsync(long productId, CreateVariantTypeRequest request, CancellationToken ct = default);
    Task<Result> DeleteVariantTypeAsync(long typeId, CancellationToken ct = default);

    Task<Result> AddVariantOptionAsync(long typeId, CreateVariantOptionRequest request, CancellationToken ct = default);
    Task<Result> DeleteVariantOptionAsync(long optionId, CancellationToken ct = default);

    Task<Result> RegenerateVariantsAsync(long productId, CancellationToken ct = default);
    Task<Result> BulkUpdateVariantsAsync(long productId, BulkUpdateVariantsRequest request, CancellationToken ct = default);
    Task<Result> UpdateVariantAsync(long variantId, UpdateVariantRequest request, CancellationToken ct = default);
    Task<Result> DeleteVariantAsync(long variantId, CancellationToken ct = default);
}
