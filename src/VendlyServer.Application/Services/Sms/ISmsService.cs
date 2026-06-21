using VendlyServer.Application.Services.Sms.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Sms;

public interface ISmsService
{
    // SMS yuboradi va natijani bazaga log qiladi.
    Task<Result<SmsResponse>> SendAsync(SendSmsRequest request, CancellationToken cancellationToken = default);

    // Eskiz balansi (so'm).
    Task<Result<decimal>> GetBalanceAsync(CancellationToken cancellationToken = default);

    // Log yozuvini id bo'yicha olish.
    Task<Result<SmsResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    // Eskiz status callback'ini qayta ishlash (RequestId bo'yicha statusni yangilaydi, idempotent).
    Task<Result> HandleCallbackAsync(SmsStatusCallbackRequest callback, CancellationToken cancellationToken = default);
}
