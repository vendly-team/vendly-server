using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Payments.Payme.Contracts;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Infrastructure.Payments.Payme;

// Payme (Paycom) merchant API: base64 checkout redirect + bitta JSON-RPC 2.0 webhook
// (Basic auth bilan himoyalangan). Holat mashinasi (spec):
// Created(1) -> Completed(2) | Cancelled(-1), Completed -> CancelledAfterComplete(-2).
// Bizda PaymentOrder rolini Payment, PaymentOrderTransaction rolini PaymentTransaction bajaradi.
public class PaymeProvider(
    AppDbContext dbContext,
    IOptions<PaymeOptions> paymeOptions,
    IOptions<PaymentsOptions> paymentsOptions,
    ILogger<PaymeProvider> logger) : IPaymentProvider
{
    // Created tranzaksiya 12 soatdan keyin eskirgan hisoblanadi (spec timeout).
    private static readonly TimeSpan TransactionTimeout = TimeSpan.FromHours(12);

    private readonly PaymeOptions _config = paymeOptions.Value;
    private readonly PaymentsOptions _payments = paymentsOptions.Value;

    public string Name => "payme";

    public string CreatePaymentUrl(Order order)
    {
        // Payme summani tiyinda kutadi (so'm * 100).
        var amountTiyin = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
        var returnUrl = $"{_payments.ReturnUrl}?order={order.OrderNumber}";
        var payload = $"m={_config.MerchantId};ac.order_id={order.Id};a={amountTiyin};l=uz;c={returnUrl}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        return $"{_config.CheckoutBase.TrimEnd('/')}/{encoded}";
    }

    public async Task<IResult> HandleWebhookAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        PaymeWebhookRequest? rpc;
        try
        {
            rpc = await JsonSerializer.DeserializeAsync<PaymeWebhookRequest>(
                request.Body, PaymentJson.Options, cancellationToken);
        }
        catch (JsonException)
        {
            rpc = null;
        }

        if (rpc is null)
            return Error(0, PaymeErrorCode.InvalidJsonRpc, "Noto'g'ri so'rov", "Неверный запрос", "Invalid request");

        if (!IsAuthorized(request))
        {
            logger.LogWarning("Payme webhook: basic auth failed (method={Method})", rpc.Method);
            return Error(rpc.Id, PaymeErrorCode.InsufficientPrivilege,
                "Avtorizatsiya xatosi", "Ошибка авторизации", "Insufficient privilege");
        }

        logger.LogInformation(
            "Payme webhook: method={Method} tx={TransactionId} order={OrderId} amount={Amount}",
            rpc.Method, rpc.Params.Id, rpc.Params.Account?.OrderId, rpc.Params.Amount);

        return rpc.Method switch
        {
            "CheckPerformTransaction" => await CheckPerformAsync(rpc, cancellationToken),
            "CreateTransaction" => await CreateTransactionAsync(rpc, cancellationToken),
            "PerformTransaction" => await PerformTransactionAsync(rpc, cancellationToken),
            "CancelTransaction" => await CancelTransactionAsync(rpc, cancellationToken),
            "CheckTransaction" => await CheckTransactionAsync(rpc, cancellationToken),
            "GetStatement" => await GetStatementAsync(rpc, cancellationToken),
            _ => Error(rpc.Id, PaymeErrorCode.MethodNotFound,
                "Metod topilmadi", "Метод не найден", "Method not found"),
        };
    }

    private bool IsAuthorized(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            return decoded == $"{_config.Login}:{_config.Password}";
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task<IResult> CheckPerformAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        var (payment, error) = await ResolvePaymentAsync(rpc, ct);
        if (error is not null) return error;

        var busy = await HasOtherActiveTransactionAsync(payment!.Id, excludeTransactionId: null, ct);
        if (busy)
            return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                "Buyurtmada faol tranzaksiya bor", "У заказа есть активная транзакция", "Order has an active transaction");

        return Result(rpc.Id, new PaymeCheckPerformResponse { Allow = true });
    }

    private async Task<IResult> CreateTransactionAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rpc.Params.Id))
            return Error(rpc.Id, PaymeErrorCode.TransactionNotFound,
                "Tranzaksiya topilmadi", "Транзакция не найдена", "Transaction not found");

        var existing = await dbContext.PaymentTransactions
            .SingleOrDefaultAsync(
                t => t.Provider == PaymentProvider.Payme && t.ProviderTransactionId == rpc.Params.Id, ct);

        if (existing is not null)
        {
            if (existing.State != PaymentTransactionState.Created)
                return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                    "Tranzaksiya holati noto'g'ri", "Неверное состояние транзакции", "Invalid transaction state");

            if (IsExpired(existing))
            {
                await CancelByTimeoutAsync(existing, ct);
                return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                    "Tranzaksiya muddati o'tgan", "Транзакция просрочена", "Transaction timed out");
            }

            return Result(rpc.Id, new PaymeCreateTransactionResponse
            {
                CreateTime = existing.CreateTime.ToUnixTimeMilliseconds(),
                Transaction = existing.Id.ToString(),
                State = (int)existing.State,
            });
        }

        var (payment, error) = await ResolvePaymentAsync(rpc, ct);
        if (error is not null) return error;

        var busy = await HasOtherActiveTransactionAsync(payment!.Id, excludeTransactionId: rpc.Params.Id, ct);
        if (busy)
            return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                "Buyurtmada faol tranzaksiya bor", "У заказа есть активная транзакция", "Order has an active transaction");

        var amountTiyin = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var transaction = new PaymentTransaction
        {
            PaymentId = payment.Id,
            Provider = PaymentProvider.Payme,
            ProviderTransactionId = rpc.Params.Id,
            State = PaymentTransactionState.Created,
            Amount = amountTiyin,
            PaymeTime = rpc.Params.Time,
            CreateTime = DateTimeOffset.UtcNow,
        };
        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(ct);

        return Result(rpc.Id, new PaymeCreateTransactionResponse
        {
            CreateTime = transaction.CreateTime.ToUnixTimeMilliseconds(),
            Transaction = transaction.Id.ToString(),
            State = (int)transaction.State,
        });
    }

    private async Task<IResult> PerformTransactionAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        var transaction = await FindTransactionAsync(rpc.Params.Id, ct);
        if (transaction is null)
            return Error(rpc.Id, PaymeErrorCode.TransactionNotFound,
                "Tranzaksiya topilmadi", "Транзакция не найдена", "Transaction not found");

        // Idempotent: allaqachon bajarilgan bo'lsa o'sha natija qaytadi.
        if (transaction.State == PaymentTransactionState.Completed)
            return Result(rpc.Id, new PaymePerformTransactionResponse
            {
                Transaction = transaction.Id.ToString(),
                PerformTime = transaction.PerformTime?.ToUnixTimeMilliseconds() ?? 0,
                State = (int)transaction.State,
            });

        if (transaction.State != PaymentTransactionState.Created)
            return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                "Tranzaksiya bekor qilingan", "Транзакция отменена", "Transaction is cancelled");

        if (IsExpired(transaction))
        {
            await CancelByTimeoutAsync(transaction, ct);
            return Error(rpc.Id, PaymeErrorCode.CouldNotPerform,
                "Tranzaksiya muddati o'tgan", "Транзакция просрочена", "Transaction timed out");
        }

        transaction.State = PaymentTransactionState.Completed;
        transaction.PerformTime = DateTimeOffset.UtcNow;
        PaymentStatusTransition.MarkPaid(
            transaction.Payment, transaction.ProviderTransactionId, "Payment confirmed via Payme");
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Payme payment completed: payment={PaymentId} amount={Amount}", transaction.PaymentId, transaction.Amount);

        return Result(rpc.Id, new PaymePerformTransactionResponse
        {
            Transaction = transaction.Id.ToString(),
            PerformTime = transaction.PerformTime.Value.ToUnixTimeMilliseconds(),
            State = (int)transaction.State,
        });
    }

    private async Task<IResult> CancelTransactionAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        var transaction = await FindTransactionAsync(rpc.Params.Id, ct);
        if (transaction is null)
            return Error(rpc.Id, PaymeErrorCode.TransactionNotFound,
                "Tranzaksiya topilmadi", "Транзакция не найдена", "Transaction not found");

        var reason = rpc.Params.Reason is { } r && Enum.IsDefined(typeof(PaymentTransactionCancelReason), r)
            ? (PaymentTransactionCancelReason)r
            : PaymentTransactionCancelReason.UnknownError;

        switch (transaction.State)
        {
            case PaymentTransactionState.Created:
                // To'lovdan oldin bekor: tranzaksiya bekor + Payment failed (order qayta urinishga ochiq).
                transaction.State = PaymentTransactionState.Cancelled;
                transaction.CancelTime = DateTimeOffset.UtcNow;
                transaction.CancelReason = reason;
                PaymentStatusTransition.MarkFailed(transaction.Payment);
                await dbContext.SaveChangesAsync(ct);
                break;

            case PaymentTransactionState.Completed:
                // To'langan to'lovni qaytarish (refund) hozircha qo'llab-quvvatlanmaydi — keyin qo'shiladi.
                return Error(rpc.Id, PaymeErrorCode.CouldNotCancel,
                    "To'lovni qaytarib bo'lmaydi", "Возврат невозможен", "Refund is not supported");

                // Cancelled / CancelledAfterComplete — idempotent, saqlangan natija qaytadi.
        }

        return Result(rpc.Id, new PaymeCancelTransactionResponse
        {
            Transaction = transaction.Id.ToString(),
            CancelTime = transaction.CancelTime?.ToUnixTimeMilliseconds() ?? 0,
            State = (int)transaction.State,
        });
    }

    private async Task<IResult> CheckTransactionAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        var transaction = await FindTransactionAsync(rpc.Params.Id, ct);
        if (transaction is null)
            return Error(rpc.Id, PaymeErrorCode.TransactionNotFound,
                "Tranzaksiya topilmadi", "Транзакция не найдена", "Transaction not found");

        return Result(rpc.Id, new PaymeCheckTransactionResponse
        {
            CreateTime = transaction.CreateTime.ToUnixTimeMilliseconds(),
            PerformTime = transaction.PerformTime?.ToUnixTimeMilliseconds() ?? 0,
            CancelTime = transaction.CancelTime?.ToUnixTimeMilliseconds() ?? 0,
            Transaction = transaction.Id.ToString(),
            State = (int)transaction.State,
            Reason = (int?)transaction.CancelReason,
        });
    }

    private async Task<IResult> GetStatementAsync(PaymeWebhookRequest rpc, CancellationToken ct)
    {
        var from = rpc.Params.From ?? 0;
        var to = rpc.Params.To ?? long.MaxValue;

        var items = await dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.Provider == PaymentProvider.Payme
                        && t.PaymeTime != null && t.PaymeTime >= from && t.PaymeTime <= to)
            .OrderBy(t => t.PaymeTime)
            .Select(t => new PaymeStatementItemResponse
            {
                Id = t.ProviderTransactionId,
                Time = t.PaymeTime!.Value,
                Amount = t.Amount,
                Account = new PaymeWebhookAccountRequest { OrderId = t.Payment.OrderId.ToString() },
                CreateTime = t.CreateTime.ToUnixTimeMilliseconds(),
                PerformTime = t.PerformTime != null ? t.PerformTime.Value.ToUnixTimeMilliseconds() : 0,
                CancelTime = t.CancelTime != null ? t.CancelTime.Value.ToUnixTimeMilliseconds() : 0,
                Transaction = t.Id.ToString(),
                State = (int)t.State,
                Reason = (int?)t.CancelReason,
            })
            .ToListAsync(ct);

        return Result(rpc.Id, new PaymeStatementResponse { Transactions = items });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // ac.order_id (= Order.Id) + summani tekshiradi; Pending Payment yoki JSON-RPC error qaytaradi.
    private async Task<(Payment? Payment, IResult? Error)> ResolvePaymentAsync(
        PaymeWebhookRequest rpc, CancellationToken ct)
    {
        if (!long.TryParse(rpc.Params.Account?.OrderId, out var orderId))
            return (null, Error(rpc.Id, PaymeErrorCode.InvalidAccount,
                "Buyurtma topilmadi", "Заказ не найден", "Order not found"));

        var payment = await dbContext.Payments
            .Include(p => p.Order)
            .SingleOrDefaultAsync(p => p.OrderId == orderId && !p.Order.IsDeleted, ct);

        if (payment is null || payment.Provider != PaymentProvider.Payme)
            return (null, Error(rpc.Id, PaymeErrorCode.InvalidAccount,
                "Buyurtma topilmadi", "Заказ не найден", "Order not found"));

        if (payment.Status != PaymentStatus.Pending)
            return (null, Error(rpc.Id, PaymeErrorCode.InvalidAccount,
                "Buyurtma to'lovga tayyor emas", "Заказ не ожидает оплаты", "Order is not awaiting payment"));

        // Default 0% — aniq moslik (Payme spec). CommissionPercent berilsa baza+komissiyagacha qabul.
        var amount = rpc.Params.Amount ?? 0;
        var expectedTiyin = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var maxWithCommission = (long)Math.Ceiling(expectedTiyin * (1m + _config.CommissionPercent / 100m));
        if (amount < expectedTiyin || amount > maxWithCommission)
            return (null, Error(rpc.Id, PaymeErrorCode.InvalidAmount,
                "Noto'g'ri summa", "Неверная сумма", "Invalid amount"));

        return (payment, null);
    }

    private Task<PaymentTransaction?> FindTransactionAsync(string? transactionId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(transactionId))
            return Task.FromResult<PaymentTransaction?>(null);

        return dbContext.PaymentTransactions
            .Include(t => t.Payment).ThenInclude(p => p.Order)
            .SingleOrDefaultAsync(
                t => t.Provider == PaymentProvider.Payme && t.ProviderTransactionId == transactionId, ct);
    }

    private Task<bool> HasOtherActiveTransactionAsync(
        long paymentId, string? excludeTransactionId, CancellationToken ct)
    {
        return dbContext.PaymentTransactions
            .AsNoTracking()
            .AnyAsync(t => t.PaymentId == paymentId
                           && t.State == PaymentTransactionState.Created
                           && (excludeTransactionId == null || t.ProviderTransactionId != excludeTransactionId), ct);
    }

    private static bool IsExpired(PaymentTransaction transaction)
    {
        var createdAt = transaction.PaymeTime is { } ms
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms)
            : transaction.CreateTime;
        return DateTimeOffset.UtcNow - createdAt > TransactionTimeout;
    }

    private async Task CancelByTimeoutAsync(PaymentTransaction transaction, CancellationToken ct)
    {
        transaction.State = PaymentTransactionState.Cancelled;
        transaction.CancelTime = DateTimeOffset.UtcNow;
        transaction.CancelReason = PaymentTransactionCancelReason.TimedOut;

        var payment = transaction.Payment
            ?? await dbContext.Payments.SingleAsync(p => p.Id == transaction.PaymentId, ct);
        PaymentStatusTransition.MarkFailed(payment);

        await dbContext.SaveChangesAsync(ct);
    }

    private static IResult Result(long id, object result) =>
        Results.Json(new PaymeRpcResultResponse { Id = id, Result = result }, PaymentJson.Options);

    private IResult Error(long id, PaymeErrorCode code, string uz, string ru, string en)
    {
        logger.LogInformation("Payme webhook error response: id={Id} code={Code} message={Message}", id, (int)code, en);
        return Results.Json(new PaymeRpcResultResponse
        {
            Id = id,
            Error = new PaymeErrorResponse
            {
                Code = (int)code,
                Message = new PaymeErrorMessageResponse { Uz = uz, Ru = ru, En = en },
                Data = "order_id",
            },
        }, PaymentJson.Options);
    }
}
