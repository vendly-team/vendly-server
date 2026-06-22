using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Hamkor.Contracts;

namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public interface IHamkorBroker
{
    /// <summary>Creates a hosted payment page and returns its URL (pay.create.url).</summary>
    Task<Result<string>> CreatePaymentUrlAsync(
        HamkorCreatePaymentUrlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a payment by external_id to confirm its state (pay.get.inv).</summary>
    Task<Result<HamkorInvoiceResult>> GetByExtIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);
}
