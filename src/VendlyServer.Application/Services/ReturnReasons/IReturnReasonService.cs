using VendlyServer.Application.Services.ReturnReasons.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.ReturnReasons;

public interface IReturnReasonService
{
    Task<Result<List<ReturnReasonResponse>>> GetAllAsync(ReturnReasonFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<ReturnReasonResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(CreateReturnReasonRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, CreateReturnReasonRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
