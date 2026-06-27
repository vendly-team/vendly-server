using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Payments.Click;

// Click Merchant API v2 — server-to-server outbound chaqiruvlar.
// Shop API (redirect + webhook) ClickProvider'da; bu broker faqat polling/refund kabi
// active so'rovlar uchun.
public interface IClickBroker
{
    Task<Result<ClickPaymentStatus>> GetPaymentStatusAsync(
        long clickTransId,
        CancellationToken cancellationToken = default);
}
