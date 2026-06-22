using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Pricing;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Orders;

public class OrderService(
    AppDbContext dbContext,
    IOrderShippingService shippingService,
    IShippingCalculatorService shippingCalculatorService,
    IProductPricingService pricingService) : IOrderService
{
    // ── Customer — checkout flow ──────────────────────────────────────────────

    public async Task<Result<CreateOrderResponse>> CreateDraftAsync(
        long userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == request.AddressId && a.UserId == userId && !a.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (address is null) return OrderErrors.AddressNotFound;

        // Money path — USD rate / category narx mavjud bo'lmasa checkout to'xtaydi (raw fallback YO'Q).
        var pricingResult = await pricingService.CreateContextAsync(cancellationToken);
        if (!pricingResult.IsSuccess) return pricingResult.Error;
        var pricing = pricingResult.Data!;

        // Bir vaqtda faqat bitta to'lanmagan (Draft/New) order bo'ladi. Yangisini YARATMAYMIZ —
        // mavjudini davom ettiramiz: addressni yangilab, o'z cartidan re-sync qilamiz, o'shani qaytaramiz.
        var existing = await dbContext.Orders
            .Where(o => o.UserId == userId && !o.IsDeleted &&
                        (o.Status == OrderStatus.Draft || o.Status == OrderStatus.New))
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Cart)
                .ThenInclude(c => c!.Items.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
            .Include(o => o.Cart)
                .ThenInclude(c => c!.Items.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Measurements)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var existingItems = existing.Cart?.Items.Where(i => !i.IsDeleted).ToList() ?? [];
            if (existingItems.Count == 0) return OrderErrors.CartEmpty;

            var existingQuote = await QuoteForCartAsync(existingItems, address, cancellationToken);
            if (!existingQuote.IsSuccess) return existingQuote.Error;

            ApplyAddress(existing, address);
            OrderItemSync.Apply(existing, existingItems, pricing, existingQuote.Data!.Cost);

            // To'lov boshlangan (New) bo'lsa — address/itemlar o'zgardi → eski Hamkor summasi eskirdi → Draft.
            if (existing.Status == OrderStatus.New)
            {
                existing.Status = OrderStatus.Draft;
                existing.StatusHistory.Add(new OrderStatusHistory
                {
                    Status = OrderStatus.Draft,
                    Note = "Reverted to draft on checkout resume",
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return new CreateOrderResponse(existing.Id, existing.OrderNumber);
        }

        // Open cart (hali orderga biriktirilmagan)
        var cart = await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted && !c.IsCheckedOut)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Measurements)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var items = cart?.Items.Where(i => !i.IsDeleted).ToList() ?? [];
        if (cart is null || items.Count == 0) return OrderErrors.CartEmpty;

        var quote = await QuoteForCartAsync(items, address, cancellationToken);
        if (!quote.IsSuccess) return quote.Error;

        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Draft,
            CartId = cart.Id,
            DiscountAmount = 0,
            DeliveryCity = address.City,
            DeliveryDistrict = address.District,
            DeliveryStreet = address.Street,
            DeliveryHouse = address.House,
            DeliveryExtra = address.Extra,
            DeliveryBtsCityCode = address.BtsCityCode,
            DeliveryBtsBranchCode = address.BtsBranchCode,
        };

        OrderItemSync.Apply(order, items, pricing, quote.Data!.Cost);
        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Draft });

        // Cart endi shu orderga biriktirildi → keyingi GET /api/carts yangi bo'sh savat yaratadi.
        cart.IsCheckedOut = true;

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(order.Id, order.OrderNumber);
    }

    // Foydalanuvchining barcha "active" (terminal bo'lmagan) orderlari: Draft/New + bajarilayotganlar.
    public async Task<Result<List<OrderListItemResponse>>> GetActiveOrdersAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        var active = new[]
        {
            OrderStatus.Draft, OrderStatus.New, OrderStatus.Payed, OrderStatus.Preparing,
            OrderStatus.Shipped, OrderStatus.InTransit, OrderStatus.OutForDelivery,
        };

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted && active.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Payment)
            .ToListAsync(cancellationToken);

        return orders.Select(o => MapToListItem(o, includeCustomer: false)).ToList();
    }

    public async Task<Result> CancelDraftAsync(
        long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Where(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted &&
                        (o.Status == OrderStatus.Draft || o.Status == OrderStatus.New))
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;

        order.Status = OrderStatus.Cancelled;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = "Cancelled by customer",
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetAddressAsync(
        long userId, long id, SetOrderAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == request.AddressId && a.UserId == userId && !a.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (address is null) return OrderErrors.AddressNotFound;

        var order = await dbContext.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Where(o => o.Id == id && o.UserId == userId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;
        if (order.Status != OrderStatus.Draft) return OrderErrors.NotDraft;

        var weight = (double)order.Items.Where(i => !i.IsDeleted).Sum(i => i.WeightKgSnap * i.Qty);
        if (order.Items.Where(i => !i.IsDeleted).Any(i => i.WeightKgSnap <= 0) || weight <= 0)
            return ShippingErrors.WeightMissing;

        var quoteResult = await shippingCalculatorService.CalculateAsync(
            new ShippingQuoteRequest(address.BtsCityCode, address.BtsBranchCode, weight), cancellationToken);
        if (!quoteResult.IsSuccess) return quoteResult.Error;

        ApplyAddress(order, address);
        order.DeliveryCost = quoteResult.Data!.Cost;
        order.TotalAmount = order.Subtotal + quoteResult.Data.Cost;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // Manzil tanlanganda checkoutdan oldin real-vaqt yetkazib berish narxini ko'rsatish uchun.
    // Og'irlik server tomonda foydalanuvchining ochiq savatidan hisoblanadi (mijozga ishonilmaydi).
    public async Task<Result<ShippingQuoteResponse>> QuoteForAddressAsync(
        long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (address is null) return OrderErrors.AddressNotFound;

        var cart = await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted && !c.IsCheckedOut)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Measurements)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var items = cart?.Items.Where(i => !i.IsDeleted).ToList() ?? [];
        if (cart is null || items.Count == 0) return OrderErrors.CartEmpty;

        return await QuoteForCartAsync(items, address, cancellationToken);
    }

    // Cart itemlaridan og'irlik hisoblab BTS quote oladi. Biror item og'irligi yoki jami og'irlik <=0 → WeightMissing.
    private async Task<Result<ShippingQuoteResponse>> QuoteForCartAsync(
        List<CartItem> items, Domain.Entities.Ref.Address address, CancellationToken cancellationToken)
    {
        var active = items.Where(i => !i.IsDeleted).ToList();
        var weight = (double)active.Sum(i => (i.ProductVariant.Measurements?.WeightKg ?? 0) * i.Qty);

        if (active.Any(i => (i.ProductVariant.Measurements?.WeightKg ?? 0) <= 0) || weight <= 0)
            return ShippingErrors.WeightMissing;

        return await shippingCalculatorService.CalculateAsync(
            new ShippingQuoteRequest(address.BtsCityCode, address.BtsBranchCode, weight), cancellationToken);
    }

    // ── Customer — read ───────────────────────────────────────────────────────

    public async Task<Result<List<OrderListItemResponse>>> GetMyOrdersAsync(
        long userId, OrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted);

        query = ApplyStatusFilter(query, filter.Status);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Payment)
            .ToListAsync(cancellationToken);

        return orders.Select(o => MapToListItem(o, includeCustomer: false)).ToList();
    }

    public async Task<Result<OrderResponse>> GetMyByIdAsync(
        long userId, long id, CancellationToken cancellationToken = default)
    {
        var order = await QueryDetail()
            .Where(o => o.Id == id && o.UserId == userId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return order is null
            ? OrderErrors.NotFound
            : MapToResponse(order, includeNotes: false, includeCustomer: false);
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public async Task<Result<List<OrderListItemResponse>>> GetAllAsync(
        OrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted);

        query = ApplyStatusFilter(query, filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.User.FirstName.Contains(term) ||
                o.User.LastName.Contains(term));
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Payment)
            .Include(o => o.User)
            .ToListAsync(cancellationToken);

        return orders.Select(o => MapToListItem(o, includeCustomer: true)).ToList();
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await QueryDetail()
            .Include(o => o.Notes.Where(n => !n.IsDeleted))
            .Include(o => o.User)
            .Where(o => o.Id == id && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return order is null
            ? OrderErrors.NotFound
            : MapToResponse(order, includeNotes: true, includeCustomer: true);
    }

    public async Task<Result<OrderResponse>> UpdateStatusAsync(
        long adminId, long id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var target))
            return OrderErrors.UnknownStatus;

        // Cancellation has its own flow (reason + BTS cancel).
        if (target == OrderStatus.Cancelled)
            return OrderErrors.InvalidTransition;

        var order = await dbContext.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Payment)
            .Include(o => o.User)
            .Where(o => o.Id == id && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;

        if (order.Payment is null || order.Payment.Status != PaymentStatus.Paid)
            return OrderErrors.NotPaid;

        if (!OrderStatusTransitions.IsAllowed(order.Status, target))
            return OrderErrors.InvalidTransition;

        // Entering "Shipped" creates the BTS delivery.
        if (target == OrderStatus.Shipped)
        {
            var shipResult = await shippingService.ShipAsync(order, cancellationToken);
            if (shipResult.IsFailure)
                return OrderErrors.ShippingFailed;
        }

        order.Status = target;
        if (target == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = target,
            Note = request.Note,
            ChangedBy = adminId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<OrderNoteResponse>> AddNoteAsync(
        long adminId, long id, AddOrderNoteRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Where(o => o.Id == id && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;

        var note = new OrderNote
        {
            OrderId = order.Id,
            AdminId = adminId,
            Note = request.Note,
        };
        dbContext.OrderNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderNoteResponse(note.Id, note.Note, note.CreatedAt);
    }

    public async Task<Result<OrderResponse>> CancelAsync(
        long adminId, string role, long id, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Payment)
            .Where(o => o.Id == id && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;

        if (!OrderStatusTransitions.IsCancellable(order.Status))
            return OrderErrors.NotCancellable;

        // Cancel the BTS delivery if one was created (best-effort).
        if (!string.IsNullOrWhiteSpace(order.BtsOrderId))
            await shippingService.CancelShipmentAsync(order, cancellationToken);

        var reasonCode = Enum.TryParse<CancellationReasonCode>(request.ReasonCode, ignoreCase: true, out var rc)
            ? rc
            : CancellationReasonCode.Other;

        dbContext.OrderCancellations.Add(new OrderCancellation
        {
            OrderId = order.Id,
            RequestedBy = adminId,
            ReasonCode = reasonCode,
            ReasonText = request.ReasonText,
            CancelledByRole = role,
        });

        order.Status = OrderStatus.Cancelled;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = request.ReasonText,
            ChangedBy = adminId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<string>> GetStickerAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Id == id && !o.IsDeleted)
            .Select(o => new { o.BtsStickerUrl })
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;
        if (string.IsNullOrWhiteSpace(order.BtsStickerUrl)) return OrderErrors.StickerNotAvailable;

        return order.BtsStickerUrl;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static void ApplyAddress(Order order, Domain.Entities.Ref.Address address)
    {
        order.DeliveryCity = address.City;
        order.DeliveryDistrict = address.District;
        order.DeliveryStreet = address.Street;
        order.DeliveryHouse = address.House;
        order.DeliveryExtra = address.Extra;
        order.DeliveryBtsCityCode = address.BtsCityCode;
        order.DeliveryBtsBranchCode = address.BtsBranchCode;
    }

    private IQueryable<Order> QueryDetail() =>
        dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .Include(o => o.StatusHistory)
            .Include(o => o.Payment);

    private static IQueryable<Order> ApplyStatusFilter(IQueryable<Order> query, string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }
        return query;
    }

    private static OrderListItemResponse MapToListItem(Order order, bool includeCustomer) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            (order.Payment?.Status ?? PaymentStatus.Pending).ToString(),
            order.DeliveryStatus.ToString(),
            order.Subtotal,
            order.DeliveryCost,
            order.TotalAmount,
            order.Items.Count(i => !i.IsDeleted),
            order.DeliveryCity,
            order.Items
                .Where(i => !i.IsDeleted)
                .Select(i => new OrderItemResponse(i.Id, i.ProductId, i.ProductNameSnap, i.SkuSnap, i.ImageSnap, i.Qty, i.PriceSnap, i.TotalSnap))
                .ToList(),
            includeCustomer && order.User is not null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : null,
            order.CreatedAt);

    private static OrderResponse MapToResponse(Order order, bool includeNotes, bool includeCustomer) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            (order.Payment?.Status ?? PaymentStatus.Pending).ToString(),
            order.DeliveryStatus.ToString(),
            order.Subtotal,
            order.DeliveryCost,
            order.DiscountAmount,
            order.TotalAmount,
            new OrderDeliveryResponse(
                order.DeliveryCity,
                order.DeliveryDistrict,
                order.DeliveryStreet,
                order.DeliveryHouse,
                order.DeliveryExtra,
                order.DeliveryBtsCityCode,
                order.BtsBarcode,
                order.BtsTrackingUrl,
                order.BtsStickerUrl,
                order.BtsLastStatusName,
                order.DeliveredAt),
            order.Payment is null
                ? null
                : new OrderPaymentResponse(
                    order.Payment.Provider.ToString(),
                    order.Payment.Status.ToString(),
                    order.Payment.Amount,
                    order.Payment.PaidAt),
            order.Items
                .Where(i => !i.IsDeleted)
                .Select(i => new OrderItemResponse(
                    i.Id, i.ProductId, i.ProductNameSnap, i.SkuSnap, i.ImageSnap, i.Qty, i.PriceSnap, i.TotalSnap))
                .ToList(),
            order.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(h => new OrderStatusHistoryResponse(h.Status.ToString(), h.Note, h.CreatedAt))
                .ToList(),
            includeNotes
                ? order.Notes.Where(n => !n.IsDeleted)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new OrderNoteResponse(n.Id, n.Note, n.CreatedAt))
                    .ToList()
                : [],
            includeCustomer && order.User is not null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : null,
            order.CreatedAt);
}
