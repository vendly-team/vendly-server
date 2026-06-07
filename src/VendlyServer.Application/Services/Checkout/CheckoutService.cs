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
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Checkout;

public class CheckoutService(
    AppDbContext dbContext,
    IHamkorBroker hamkorBroker,
    IOptions<ClientOptions> clientOptions,
    IOptions<HamkorOptions> hamkorOptions,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    // Mirrors the frontend DELIVERY_COST constant; move to delivery calculation/config later.
    private const decimal DeliveryCost = 10m;

    private readonly ClientOptions _client = clientOptions.Value;
    private readonly HamkorOptions _hamkor = hamkorOptions.Value;

    public async Task<Result<CheckoutResponse>> CreateAsync(
        long userId, CreateCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == request.AddressId && a.UserId == userId && !a.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (address is null) return CheckoutErrors.AddressNotFound;

        var cart = await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Measurements)
            .SingleOrDefaultAsync(cancellationToken);

        var items = cart?.Items.Where(i => !i.IsDeleted).ToList() ?? [];
        if (items.Count == 0) return CheckoutErrors.CartEmpty;

        var subtotal = items.Sum(i => i.ProductVariant.Price * i.Qty);
        var totalAmount = subtotal + DeliveryCost;

        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            Status = OrderStatus.New,
            Subtotal = subtotal,
            DeliveryCost = DeliveryCost,
            DiscountAmount = 0,
            TotalAmount = totalAmount,
            DeliveryCity = address.City,
            DeliveryDistrict = address.District,
            DeliveryStreet = address.Street,
            DeliveryHouse = address.House,
            DeliveryExtra = address.Extra,
            DeliveryBtsCityCode = address.BtsCityCode,
            Payment = new Payment
            {
                Provider = PaymentProvider.Hamkor,
                Status = PaymentStatus.Pending,
                Amount = totalAmount
            }
        };

        foreach (var item in items)
        {
            var variant = item.ProductVariant;
            order.Items.Add(new OrderItem
            {
                ProductId = variant.ProductId,
                ProductNameSnap = variant.Product.Name.Uz ?? variant.Product.Name.Ru ?? string.Empty,
                SkuSnap = string.IsNullOrWhiteSpace(variant.Name) ? $"VAR-{variant.Id}" : variant.Name,
                ImageSnap = variant.Images.FirstOrDefault() ?? string.Empty,
                WeightKgSnap = variant.Measurements?.WeightKg ?? 0,
                Qty = item.Qty,
                PriceSnap = variant.Price,
                TotalSnap = variant.Price * item.Qty
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.New });

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var clientBase = _client.BaseUrl.TrimEnd('/');
        var successUrl = $"{clientBase}/payment/success?order={orderNumber}";
        var failureUrl = $"{clientBase}/payment/fail?order={orderNumber}";
        var callbackUrl = $"{_hamkor.CallbackBaseUrl.TrimEnd('/')}/api/hamkor/webhook";

        // Hamkor expects the amount in tiyin (smallest currency unit) = so'm * 100.
        var amountMinorUnits = (long)Math.Round(totalAmount * 100m, MidpointRounding.AwayFromZero);

        // Build fiscal lines for each cart item plus delivery; line amount is total (price * qty).
        var fiscalItems = items
            .Select(i => new HamkorCreatePaymentItem(
                AmountMinorUnits: (long)Math.Round(i.ProductVariant.Price * i.Qty * 100m, MidpointRounding.AwayFromZero),
                Qty: i.Qty))
            .Append(new HamkorCreatePaymentItem(
                AmountMinorUnits: (long)Math.Round(DeliveryCost * 100m, MidpointRounding.AwayFromZero),
                Qty: 1))
            .ToList();

        var urlResult = await hamkorBroker.CreatePaymentUrlAsync(
            new HamkorCreatePaymentUrlRequest
            {
                ExternalId = orderNumber,
                AmountMinorUnits = amountMinorUnits,
                SuccessUrl = successUrl,
                FailureUrl = failureUrl,
                CallbackUrl = callbackUrl,
                Items = fiscalItems,
            },
            cancellationToken);

        if (urlResult.IsFailure)
        {
            logger.LogError("Checkout: failed to create payment url for order {OrderNumber}", orderNumber);
            return CheckoutErrors.PaymentUrlFailed;
        }

        return new CheckoutResponse(urlResult.Data!, orderNumber);
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
            order.Status = OrderStatus.Accepted;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Accepted,
                Note = "Payment confirmed via Hamkorbank"
            });

            await ClearCartAsync(order.UserId, cancellationToken);
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

    private async Task ClearCartAsync(long userId, CancellationToken cancellationToken)
    {
        var cartItems = await dbContext.CartItems
            .Where(i => i.Cart.UserId == userId && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var item in cartItems)
            item.IsDeleted = true;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
