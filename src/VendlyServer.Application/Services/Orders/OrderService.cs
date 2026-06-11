using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Pricing;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Orders;

public class OrderService(
    AppDbContext dbContext,
    IOrderShippingService shippingService,
    IProductPricingService pricingService) : IOrderService
{
    // Mirrors the frontend DELIVERY_COST constant; move to delivery calculation/config later.
    private const decimal DeliveryCost = 10m;

    // ── Customer — checkout flow ──────────────────────────────────────────────

    public async Task<Result<CreateOrderResponse>> CreateDraftAsync(
        long userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == request.AddressId && a.UserId == userId && !a.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (address is null) return OrderErrors.AddressNotFound;

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
        if (items.Count == 0) return OrderErrors.CartEmpty;

        // Money path — USD rate / category narx mavjud bo'lmasa checkout to'xtaydi (raw fallback YO'Q).
        var pricingResult = await pricingService.CreateContextAsync(cancellationToken);
        if (!pricingResult.IsSuccess) return pricingResult.Error;
        var pricing = pricingResult.Data!;

        // Cart ga allaqachon Draft/New order bog'langan bo'lsa — yangisini yaratmasdan davom etamiz.
        if (cart is not null)
        {
            var existing = await dbContext.Orders
                .Where(o => o.CartId == cart.Id &&
                            (o.Status == OrderStatus.Draft || o.Status == OrderStatus.New) &&
                            !o.IsDeleted)
                .Select(o => new { o.Id, o.OrderNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                var activeOrder = await dbContext.Orders
                    .Where(o => o.Id == existing.Id)
                    .Include(o => o.Items.Where(i => !i.IsDeleted))
                    .SingleAsync(cancellationToken);

                activeOrder.DeliveryCity        = address.City;
                activeOrder.DeliveryDistrict    = address.District;
                activeOrder.DeliveryStreet      = address.Street;
                activeOrder.DeliveryHouse       = address.House;
                activeOrder.DeliveryExtra       = address.Extra;
                activeOrder.DeliveryBtsCityCode = address.BtsCityCode;

                // Cart da o'zgargan itemlarni orderga sync qilamiz
                foreach (var oi in activeOrder.Items)
                    oi.IsDeleted = true;

                foreach (var item in items)
                {
                    var variant = item.ProductVariant;
                    var unitPrice = pricing.CalculateSoumPrice(variant.Price, variant.Product.CategoryId);
                    activeOrder.Items.Add(new OrderItem
                    {
                        ProductId       = variant.ProductId,
                        ProductNameSnap = variant.Product.Name.Uz ?? variant.Product.Name.Ru ?? string.Empty,
                        SkuSnap         = string.IsNullOrWhiteSpace(variant.Name) ? $"VAR-{variant.Id}" : variant.Name,
                        ImageSnap       = variant.Images.FirstOrDefault() ?? string.Empty,
                        WeightKgSnap    = variant.Measurements?.WeightKg ?? 0,
                        Qty             = item.Qty,
                        PriceSnap       = unitPrice,
                        TotalSnap       = unitPrice * item.Qty,
                    });
                }

                var newSubtotal         = items.Sum(i =>
                    pricing.CalculateSoumPrice(i.ProductVariant.Price, i.ProductVariant.Product.CategoryId) * i.Qty);
                activeOrder.Subtotal    = newSubtotal;
                activeOrder.TotalAmount = newSubtotal + DeliveryCost;

                await dbContext.SaveChangesAsync(cancellationToken);
                return new CreateOrderResponse(existing.Id, existing.OrderNumber);
            }
        }

        var subtotal = items.Sum(i =>
            pricing.CalculateSoumPrice(i.ProductVariant.Price, i.ProductVariant.Product.CategoryId) * i.Qty);
        var totalAmount = subtotal + DeliveryCost;
        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Draft,
            CartId = cart?.Id,
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
        };

        foreach (var item in items)
        {
            var variant = item.ProductVariant;
            var unitPrice = pricing.CalculateSoumPrice(variant.Price, variant.Product.CategoryId);
            order.Items.Add(new OrderItem
            {
                ProductId = variant.ProductId,
                ProductNameSnap = variant.Product.Name.Uz ?? variant.Product.Name.Ru ?? string.Empty,
                SkuSnap = string.IsNullOrWhiteSpace(variant.Name) ? $"VAR-{variant.Id}" : variant.Name,
                ImageSnap = variant.Images.FirstOrDefault() ?? string.Empty,
                WeightKgSnap = variant.Measurements?.WeightKg ?? 0,
                Qty = item.Qty,
                PriceSnap = unitPrice,
                TotalSnap = unitPrice * item.Qty,
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Draft });

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(order.Id, order.OrderNumber);
    }

    public async Task<Result<CreateOrderResponse>> GetMyDraftAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        var cart = await dbContext.Carts
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (cart is null) return OrderErrors.NotFound;

        var existing = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CartId == cart.Id &&
                        (o.Status == OrderStatus.Draft || o.Status == OrderStatus.New) &&
                        !o.IsDeleted)
            .Select(o => new { o.Id, o.OrderNumber })
            .FirstOrDefaultAsync(cancellationToken);

        return existing is null
            ? OrderErrors.NotFound
            : new CreateOrderResponse(existing.Id, existing.OrderNumber);
    }

    public async Task<Result> CancelMyDraftAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        var cart = await dbContext.Carts
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (cart is null) return OrderErrors.NotFound;

        var order = await dbContext.Orders
            .Where(o => o.CartId == cart.Id &&
                        o.Status == OrderStatus.Draft &&
                        !o.IsDeleted)
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
            .Where(o => o.Id == id && o.UserId == userId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null) return OrderErrors.NotFound;
        if (order.Status != OrderStatus.Draft) return OrderErrors.NotDraft;

        order.DeliveryCity = address.City;
        order.DeliveryDistrict = address.District;
        order.DeliveryStreet = address.Street;
        order.DeliveryHouse = address.House;
        order.DeliveryExtra = address.Extra;
        order.DeliveryBtsCityCode = address.BtsCityCode;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
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
