using VendlyServer.Application.Services.CompanyInfo.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.CompanyInfo;

public interface ICompanyInfoService
{
    Task<Result<CompanyInfoResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<CompanyInfoResponse>> UpsertAsync(UpsertCompanyInfoRequest request, CancellationToken cancellationToken = default);
}
