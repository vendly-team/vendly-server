using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Eskiz.Contracts.Responses;

namespace VendlyServer.Infrastructure.Brokers.Eskiz;

public interface IEskizBroker
{
    // Auth (token avtomatik boshqariladi — odatda alohida chaqirishga hojat yo'q).
    Task<Result> LoginAsync(CancellationToken cancellationToken = default);

    // Bitta SMS yuborish. mobilePhone — 998XXXXXXXXX formatida.
    Task<Result<EskizSendResponse>> SendSmsAsync(
        string mobilePhone, string message, string? callbackUrl = null,
        CancellationToken cancellationToken = default);

    // Hisobdagi balans (so'm).
    Task<Result<decimal>> GetBalanceAsync(CancellationToken cancellationToken = default);
}
