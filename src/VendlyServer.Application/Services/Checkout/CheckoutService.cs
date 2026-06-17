using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.Hamkor;
using VendlyServer.Infrastructure.Brokers.Hamkor.Contracts;
using VendlyServer.Infrastructure.Payments;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Checkout;

public class CheckoutService(
    AppDbContext dbContext,
    IHamkorBroker hamkorBroker,
    IEnumerable<IPaymentProvider> paymentProviders,
    IOptions<ClientOptions> clientOptions,
    IOptions<HamkorOptions> hamkorOptions,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    private readonly ClientOptions _client = clientOptions.Value;
    private readonly HamkorOptions _hamkor = hamkorOptions.Value;

    public async Task<Result<CheckoutResponse>> InitiatePaymentAsync(
        long userId, long orderId, PaymentProvider provider, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Where(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return CheckoutErrors.OrderNotFound;
        if (order.Status != OrderStatus.Draft) return CheckoutErrors.NotDraft;

        string paymentUrl;
        if (provider == PaymentProvider.Hamkor)
        {
            // Hamkor: outbound broker bankdan checkout URL oladi (fiskal ma'lumot bilan).
            var hamkorUrl = await BuildHamkorUrlAsync(order, cancellationToken);
            if (hamkorUrl.IsFailure)
            {
                logger.LogError("Checkout: failed to initiate payment for order {OrderNumber}", order.OrderNumber);
                return CheckoutErrors.PaymentUrlFailed;
            }
            paymentUrl = hamkorUrl.Data!;
        }
        else
        {
            // Payme / Click: provider checkout URL'ni lokal quradi (outbound call yo'q).
            var paymentProvider = paymentProviders.SingleOrDefault(
                p => p.Name.Equals(provider.ToString(), StringComparison.OrdinalIgnoreCase));
            if (paymentProvider is null) return CheckoutErrors.ProviderNotSupported;
            paymentUrl = paymentProvider.CreatePaymentUrl(order);
        }

        order.Status = OrderStatus.New;
        order.Payment = new Payment
        {
            Provider = provider,
            Status = PaymentStatus.Pending,
            Amount = order.TotalAmount,
        };
        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.New });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckoutResponse(paymentUrl, order.OrderNumber);
    }

    // Hamkor checkout URL'ini quradi: fiskal qatorlar + Tashkent vaqti + broker chaqiruvi.
    private async Task<Result<string>> BuildHamkorUrlAsync(Order order, CancellationToken cancellationToken)
    {
        var clientBase = _client.BaseUrl.TrimEnd('/');
        var successUrl = $"{clientBase}/payment/success?order={order.OrderNumber}";
        var failureUrl = $"{clientBase}/payment/fail?order={order.OrderNumber}";
        var callbackUrl = $"{_hamkor.CallbackBaseUrl.TrimEnd('/')}/api/hamkor/webhook";

        // Hamkor expects the amount in tiyin (smallest currency unit) = so'm * 100.
        var amountMinorUnits = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);

        // Build fiscal lines for each order item plus delivery; line amount is total (price * qty).
        var fiscalItems = order.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new HamkorCreatePaymentItem(
                AmountMinorUnits: (long)Math.Round(i.PriceSnap * i.Qty * 100m, MidpointRounding.AwayFromZero),
                Qty: i.Qty))
            .Append(new HamkorCreatePaymentItem(
                AmountMinorUnits: (long)Math.Round(order.DeliveryCost * 100m, MidpointRounding.AwayFromZero),
                Qty: 1))
            .ToList();

        // The bank expects "details" entries in Tashkent local time, dd.MM.yyyy format.
        // Fallback to UTC+5 offset if the OS image is missing the IANA tzdata entry.
        var tashkentTime = TryConvertToTashkent(order.CreatedAt);
        var createdAtFormatted = tashkentTime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        return await hamkorBroker.CreatePaymentUrlAsync(
            new HamkorCreatePaymentUrlRequest
            {
                ExternalId = order.OrderNumber,
                AmountMinorUnits = amountMinorUnits,
                SuccessUrl = successUrl,
                FailureUrl = failureUrl,
                CallbackUrl = callbackUrl,
                Items = fiscalItems,
                Details =
                [
                    new HamkorDetailField { Field = "created_at", Value = createdAtFormatted },
                ],
            },
            cancellationToken);
    }

    public async Task<Result> HandleCallbackAsync(
        HamkorCallbackRequest callback, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Payment)
            .Where(o => o.OrderNumber == callback.ExtId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Hamkor callback: order {ExtId} not found", callback.ExtId);
            return CheckoutErrors.OrderNotFound;
        }

        order.Payment ??= new Payment
        {
            OrderId = order.Id,
            Provider = PaymentProvider.Hamkor,
            Status = PaymentStatus.Pending,
            Amount = order.TotalAmount
        };

        // Idempotent — the bank may deliver the callback more than once.
        if (order.Payment.Status == PaymentStatus.Paid)
            return Result.Success();

        order.Payment.ProviderResponse = JsonSerializer.SerializeToDocument(callback);

        var state = (HamkorPaymentState)callback.State;

        if (state is HamkorPaymentState.Confirmed or HamkorPaymentState.Payed)
        {
            order.Payment.Status = PaymentStatus.Paid;
            order.Payment.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Payed;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Payed,
                Note = "Payment confirmed via Hamkorbank"
            });

            // Order o'z cartiga ega (allaqachon checked-out) — to'lovdan keyin u faqat status bo'yicha
            // bloklanadi. Foydalanuvchining keyingi open carti talab bo'yicha yangidan yaratiladi.
        }
        else if (state is HamkorPaymentState.Canceled)
        {
            order.Payment.Status = PaymentStatus.Failed;
        }
        else
        {
            logger.LogInformation("Hamkor callback: order {ExtId} state {State} — left pending",
                callback.ExtId, state);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<CheckoutStatusResponse>> GetStatusByOrderAsync(
        long userId, string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Payment)
            .Where(o => o.OrderNumber == orderNumber && o.UserId == userId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return CheckoutErrors.OrderNotFound;

        var paymentStatus = order.Payment?.Status.ToString() ?? PaymentStatus.Pending.ToString();
        return new CheckoutStatusResponse(order.OrderNumber, paymentStatus, order.Status.ToString());
    }

    // Try the IANA id first (Linux containers), fall back to the Windows id, then to a fixed UTC+5 offset.
    private static DateTime TryConvertToTashkent(DateTime utc)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent"));
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Central Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.AddHours(5);
            }
        }
    }
}
