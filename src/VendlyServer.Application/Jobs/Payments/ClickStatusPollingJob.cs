using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Payments;
using VendlyServer.Infrastructure.Payments.Click;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Jobs.Payments;

// Pending bo'lib qolgan Click to'lovlarini Click Merchant API'dan tekshiradi.
// Webhook miss bo'lganda (network drop) "stuck payment" muammosini hal qiladi:
// agar Click'da Success bo'lsa — Payment/Order'ni Paid'ga ko'taradi.
public class ClickStatusPollingJob(
    AppDbContext dbContext,
    IClickBroker clickBroker,
    ILogger<ClickStatusPollingJob> logger) : IClickStatusPollingJob
{
    // To'lov yaratilgandan keyin qancha vaqt o'tganidan keyin polling boshlanadi.
    // Webhook odatda darhol keladi; agar 5 min ichida kelmagan bo'lsa — biror muammo bor.
    private const int MinAgeMinutes = 5;

    // Polling muddat chegarasi: undan eski to'lovlar qaralmaydi (deyarli aniq foydalanuvchi tashlab ketgan).
    private const int MaxAgeHours = 24;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var minCreatedAt = now.AddHours(-MaxAgeHours);
        var maxCreatedAt = now.AddMinutes(-MinAgeMinutes);

        // Pending Click to'lovlari, kamida bitta PaymentTransaction'ga ega bo'lganlari (ya'ni Prepare kelgan).
        // Agar Prepare ham kelmagan bo'lsa, click_trans_id yo'q — Click'ga so'rab bo'lmaydi.
        var pending = await dbContext.Payments
            .Include(p => p.Order)
            .Include(p => p.Transactions)
            .Where(p =>
                p.Provider == PaymentProvider.Click &&
                p.Status == PaymentStatus.Pending &&
                p.CreatedAt > minCreatedAt &&
                p.CreatedAt < maxCreatedAt &&
                !p.Order.IsDeleted &&
                p.Transactions.Any())
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            logger.LogDebug("Click polling: no pending payments to check");
            return;
        }

        logger.LogInformation("Click polling: checking {Count} pending payments", pending.Count);

        var updated = 0;
        foreach (var payment in pending)
        {
            // Eng yangi transaction (odatda Prepare callback'da yaratilgani).
            var lastTransaction = payment.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .First();

            if (!long.TryParse(lastTransaction.ProviderTransactionId, out var clickTransId))
            {
                logger.LogWarning(
                    "Click polling: payment {PaymentId} has non-numeric ProviderTransactionId={Id}",
                    payment.Id, lastTransaction.ProviderTransactionId);
                continue;
            }

            var result = await clickBroker.GetPaymentStatusAsync(clickTransId, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Click polling: failed to fetch status for payment {PaymentId} click_trans_id={ClickTransId}",
                    payment.Id, clickTransId);
                continue;
            }

            switch (result.Data)
            {
                case ClickPaymentStatus.Success:
                    lastTransaction.State = PaymentTransactionState.Completed;
                    lastTransaction.PerformTime = DateTimeOffset.UtcNow;
                    PaymentStatusTransition.MarkPaid(
                        payment,
                        lastTransaction.ProviderTransactionId,
                        "Payment confirmed via Click polling");
                    updated++;
                    logger.LogInformation(
                        "Click polling: marked payment {PaymentId} as Paid (click_trans_id={ClickTransId})",
                        payment.Id, clickTransId);
                    break;

                case ClickPaymentStatus.Cancelled:
                case ClickPaymentStatus.Failed:
                case ClickPaymentStatus.Refunded:
                    lastTransaction.State = PaymentTransactionState.Cancelled;
                    lastTransaction.CancelTime = DateTimeOffset.UtcNow;
                    lastTransaction.CancelReason = PaymentTransactionCancelReason.UnknownError;
                    PaymentStatusTransition.MarkFailed(payment);
                    updated++;
                    logger.LogInformation(
                        "Click polling: marked payment {PaymentId} as Failed (status={Status})",
                        payment.Id, result.Data);
                    break;

                case ClickPaymentStatus.Pending:
                default:
                    // Hali kutilmoqda — keyingi sikldagi tekshiruvni kutamiz.
                    break;
            }
        }

        if (updated > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Click polling: checked {Total} payments, updated {Updated}",
            pending.Count, updated);
    }
}
