using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Payments.Click.Contracts;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Infrastructure.Payments.Click;

// Click SHOP API: my.click.uz'ga redirect, keyin ikkita form-encoded callback —
// Prepare (action=0) va Complete (action=1), ikkalasi ham MD5 imzolangan.
// Bizda PaymentOrder rolini Payment, PaymentOrderTransaction rolini PaymentTransaction bajaradi.
public class ClickProvider(
    AppDbContext dbContext,
    IOptions<ClickOptions> config,
    IOptions<PaymentsOptions> paymentsOptions,
    ILogger<ClickProvider> logger) : IPaymentProvider
{
    private readonly ClickOptions _config = config.Value;
    private readonly PaymentsOptions _payments = paymentsOptions.Value;

    public string Name => "click";

    public string CreatePaymentUrl(Order order)
    {
        // Click summani so'mda kutadi (tiyin emas).
        var amountSom = order.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture);
        // To'lovdan keyin foydalanuvchi brauzeri qaytadigan manzil (status sahifasi).
        var returnUrl = Uri.EscapeDataString($"{_payments.ReturnUrl}?order={order.OrderNumber}");
        return $"{_config.CheckoutBase}" +
               $"?service_id={_config.ServiceId}" +
               $"&merchant_id={_config.MerchantId}" +
               $"&amount={amountSom}" +
               $"&transaction_param={order.Id}" +
               $"&return_url={returnUrl}";
    }

    public async Task<IResult> HandleWebhookAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var payload = ClickWebhookRequest.FromForm(form);

        logger.LogInformation(
            "Click webhook: action={Action} click_trans_id={ClickTransId} merchant_trans_id={MerchantTransId} amount={Amount} error={Error}",
            payload.Action, payload.ClickTransId, payload.MerchantTransId, payload.Amount, payload.Error);

        if (string.IsNullOrEmpty(payload.ClickTransId) ||
            string.IsNullOrEmpty(payload.ServiceId) ||
            string.IsNullOrEmpty(payload.MerchantTransId) ||
            string.IsNullOrEmpty(payload.Amount) ||
            string.IsNullOrEmpty(payload.Action) ||
            string.IsNullOrEmpty(payload.SignTime) ||
            string.IsNullOrEmpty(payload.SignString))
        {
            return Respond(payload, ClickErrorCode.ErrorInRequestFromClick, "Required fields are missing");
        }

        var signSource = ClickMd5Helper.BuildSignSource(
            payload.ClickTransId, payload.ServiceId, _config.SecretKey, payload.MerchantTransId,
            payload.MerchantPrepareId, payload.Amount, payload.Action, payload.SignTime);

        if (!ClickMd5Helper.Verify(signSource, payload.SignString))
            return Respond(payload, ClickErrorCode.SignCheckFailed, "Sign check failed");

        if (!long.TryParse(payload.MerchantTransId, out var orderId))
            return Respond(payload, ClickErrorCode.UserDoesNotExist, "Order not found");

        var payment = await dbContext.Payments
            .Include(p => p.Order)
            .SingleOrDefaultAsync(p => p.OrderId == orderId && !p.Order.IsDeleted, cancellationToken);

        if (payment is null || payment.Provider != PaymentProvider.Click)
            return Respond(payload, ClickErrorCode.UserDoesNotExist, "Order not found");

        if (!decimal.TryParse(payload.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountSom))
            return Respond(payload, ClickErrorCode.IncorrectParameterAmount, "Invalid amount format");

        // Click mijozdan ~1% xizmat haqi qo'shib oladi, shuning uchun webhook summasi baza
        // narxdan baza+komissiyagacha bo'lishi mumkin. Shu oraliqni qabul qilamiz; kam to'lov rad.
        var incomingTiyin = (long)Math.Round(amountSom * 100m, MidpointRounding.AwayFromZero);
        var expectedTiyin = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var maxWithCommission = (long)Math.Ceiling(expectedTiyin * (1m + _config.CommissionPercent / 100m));
        if (incomingTiyin < expectedTiyin || incomingTiyin > maxWithCommission)
        {
            return Respond(payload, ClickErrorCode.IncorrectParameterAmount, "Incorrect amount");
        }

        return payload.Action switch
        {
            "0" => await PrepareAsync(payload, payment, cancellationToken),
            "1" => await CompleteAsync(payload, payment, cancellationToken),
            _ => Respond(payload, ClickErrorCode.ActionNotFound, "Action not found"),
        };
    }

    private async Task<IResult> PrepareAsync(
        ClickWebhookRequest payload, Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Paid)
            return Respond(payload, ClickErrorCode.AlreadyPaid, "Already paid");

        if (payment.Status != PaymentStatus.Pending)
            return Respond(payload, ClickErrorCode.TransactionCancelled, "Order is cancelled");

        // Idempotent: shu click_trans_id uchun transaction allaqachon bo'lsa, o'sha prepare id qaytadi.
        var existing = await dbContext.PaymentTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.Provider == PaymentProvider.Click && t.ProviderTransactionId == payload.ClickTransId,
                cancellationToken);

        if (existing is not null)
            return Respond(payload, ClickErrorCode.Success, "Success", prepareId: existing.Id);

        var amountTiyin = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var transaction = new PaymentTransaction
        {
            PaymentId = payment.Id,
            Provider = PaymentProvider.Click,
            ProviderTransactionId = payload.ClickTransId,
            State = PaymentTransactionState.Created,
            Amount = amountTiyin,
            CreateTime = DateTimeOffset.UtcNow,
        };
        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Respond(payload, ClickErrorCode.Success, "Success", prepareId: transaction.Id);
    }

    private async Task<IResult> CompleteAsync(
        ClickWebhookRequest payload, Payment payment, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.PaymentTransactions
            .SingleOrDefaultAsync(
                t => t.Provider == PaymentProvider.Click && t.ProviderTransactionId == payload.ClickTransId,
                cancellationToken);

        if (transaction is null || transaction.PaymentId != payment.Id)
            return Respond(payload, ClickErrorCode.TransactionDoesNotExist, "Transaction not found");

        if (long.TryParse(payload.MerchantPrepareId, out var prepareId) && prepareId != transaction.Id)
            return Respond(payload, ClickErrorCode.TransactionDoesNotExist, "Prepare id mismatch");

        // Click o'zi xato yuborgan bo'lsa (masalan user to'lovni bekor qildi) — bekor qilamiz.
        if (int.TryParse(payload.Error, out var clickError) && clickError < 0)
        {
            if (transaction.State == PaymentTransactionState.Created)
            {
                transaction.State = PaymentTransactionState.Cancelled;
                transaction.CancelTime = DateTimeOffset.UtcNow;
                transaction.CancelReason = PaymentTransactionCancelReason.UnknownError;
                if (payment.Status == PaymentStatus.Pending)
                    PaymentStatusTransition.MarkFailed(payment);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return Respond(payload, ClickErrorCode.TransactionCancelled, "Transaction cancelled");
        }

        if (transaction.State == PaymentTransactionState.Completed)
            return Respond(payload, ClickErrorCode.AlreadyPaid, "Already paid");

        if (transaction.State is PaymentTransactionState.Cancelled or PaymentTransactionState.CancelledAfterComplete)
            return Respond(payload, ClickErrorCode.TransactionCancelled, "Transaction cancelled");

        transaction.State = PaymentTransactionState.Completed;
        transaction.PerformTime = DateTimeOffset.UtcNow;
        PaymentStatusTransition.MarkPaid(payment, transaction.ProviderTransactionId, "Payment confirmed via Click");
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Click payment completed: payment={PaymentId} amount={Amount}", payment.Id, payment.Amount);

        return Respond(payload, ClickErrorCode.Success, "Success", confirmId: transaction.Id);
    }

    private IResult Respond(
        ClickWebhookRequest payload,
        ClickErrorCode error,
        string note,
        long? prepareId = null,
        long? confirmId = null)
    {
        logger.LogInformation(
            "Click webhook response: action={Action} merchant_trans_id={MerchantTransId} error={Error} note={Note} prepareId={PrepareId} confirmId={ConfirmId}",
            payload.Action, payload.MerchantTransId, (int)error, note, prepareId, confirmId);

        _ = long.TryParse(payload.ClickTransId, out var clickTransId);
        var body = new ClickWebhookResponse
        {
            ClickTransId = clickTransId,
            MerchantTransId = payload.MerchantTransId,
            MerchantPrepareId = prepareId,
            MerchantConfirmId = confirmId,
            Error = (int)error,
            ErrorNote = note,
        };
        return Results.Json(body, PaymentJson.Options);
    }
}
