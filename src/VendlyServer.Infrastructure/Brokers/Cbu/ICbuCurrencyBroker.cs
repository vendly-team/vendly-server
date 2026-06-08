using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Cbu.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Cbu;

public interface ICbuCurrencyBroker
{
    Task<Result<CbuUsdRateResponse>> GetUsdRateAsync(CancellationToken cancellationToken = default);
}
