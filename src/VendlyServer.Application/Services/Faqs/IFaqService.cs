using VendlyServer.Application.Services.Faqs.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Faqs;

public interface IFaqService
{
    Task<Result<List<FaqResponse>>> GetAllAsync(FaqFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<FaqResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(CreateFaqRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, CreateFaqRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
