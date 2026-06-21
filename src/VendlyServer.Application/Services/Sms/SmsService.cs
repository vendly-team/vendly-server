using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Sms.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Diagnostics;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.Eskiz;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Sms;

public class SmsService(AppDbContext dbContext, IEskizBroker eskizBroker) : ISmsService
{
    private static readonly Regex PhonePattern = new(@"^998\d{9}$", RegexOptions.Compiled);

    public async Task<Result<SmsResponse>> SendAsync(SendSmsRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = NormalizePhone(request.Phone);
        if (!PhonePattern.IsMatch(phone))
            return SmsErrors.InvalidPhone;

        if (string.IsNullOrWhiteSpace(request.Message))
            return SmsErrors.EmptyMessage;

        var sms = new SmsMessage
        {
            Phone = phone,
            Message = request.Message,
            UserId = request.UserId,
            Status = SmsStatus.Pending,
        };

        dbContext.SmsMessages.Add(sms);
        await dbContext.SaveChangesAsync(cancellationToken);

        // callback_url doim config'dan olinadi (broker _options.CallbackUrl'ga fallback qiladi).
        var sent = await eskizBroker.SendSmsAsync(phone, request.Message, cancellationToken: cancellationToken);

        if (sent.IsFailure || sent.Data is null)
        {
            sms.Status = SmsStatus.Failed;
            sms.ErrorMessage = sent.Error.Message ?? sent.Error.Code;
            await dbContext.SaveChangesAsync(cancellationToken);
            return sent.Error; // Eskiz xabarini (test rejimi va h.k.) client'ga uzatamiz
        }

        sms.RequestId = sent.Data.Id;
        sms.RawStatus = sent.Data.Status;
        sms.Status = SmsStatus.Waiting;
        sms.SentAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(sms);
    }

    public async Task<Result<decimal>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var balance = await eskizBroker.GetBalanceAsync(cancellationToken);
        return balance.IsSuccess ? balance.Data : SmsErrors.BalanceFailed;
    }

    public async Task<Result<SmsResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sms = await dbContext.SmsMessages
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return sms is null ? SmsErrors.NotFound : Map(sms);
    }

    public async Task<Result> HandleCallbackAsync(SmsStatusCallbackRequest callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(callback.RequestId))
            return SmsErrors.NotFound;

        var sms = await dbContext.SmsMessages
            .SingleOrDefaultAsync(x => x.RequestId == callback.RequestId && !x.IsDeleted, cancellationToken);

        if (sms is null) return SmsErrors.NotFound;

        sms.RawStatus = callback.Status;
        sms.Status = MapStatus(callback.Status);

        if (sms.Status == SmsStatus.Delivered)
            sms.DeliveredAt = callback.StatusDate ?? DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // === Helpers ===

    // Faqat raqamlarni qoldiramiz: "+998 90 123 45 67" → "998901234567".
    private static string NormalizePhone(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());

    private static SmsStatus MapStatus(string? raw) => raw?.Trim().ToUpperInvariant() switch
    {
        "DELIVRD" or "DELIVERED" => SmsStatus.Delivered,
        "EXPIRED" => SmsStatus.Expired,
        "REJECTD" or "REJECTED" => SmsStatus.Rejected,
        "UNDELIV" or "UNDELIVERABLE" or "DELETED" or "FAILED" => SmsStatus.Failed,
        _ => SmsStatus.Waiting,
    };

    private static SmsResponse Map(SmsMessage sms) =>
        new(sms.Id, sms.Phone, sms.Message, sms.Status, sms.RequestId, sms.CreatedAt);
}
