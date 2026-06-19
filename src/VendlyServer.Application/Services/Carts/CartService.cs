using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.Carts.Contracts;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Pricing;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Carts;

public class CartService(
    AppDbContext dbContext,
    IProductPricingService pricingService,
    ILogger<CartService> logger) : ICartService
{
    public async Task<Result<CartResponse>> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var (cart, _) = await ResolveActiveCartAsync(userId, cancellationToken);
        var pricing = await ResolvePricingContextAsync(cancellationToken);
        return MapToResponse(cart, pricing);
    }

    public async Task<Result<CartResponse>> AddItemAsync(long userId, CartItemRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == request.ProductVariantId && !v.IsDeleted && v.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (variant is null) return CartErrors.VariantNotFound;

        var (cart, _) = await ResolveActiveCartAsync(userId, cancellationToken);

        var existingItem = cart.Items
            .FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId && !i.IsDeleted);

        var newQty = existingItem is not null ? existingItem.Qty + request.Qty : request.Qty;
        if (newQty > variant.Quantity) return CartErrors.InsufficientStock;

        if (existingItem is not null)
        {
            existingItem.Qty = newQty;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = request.ProductVariantId,
                Qty = request.Qty,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FinishAsync(userId, cancellationToken);
    }

    public async Task<Result<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var (cart, _) = await ResolveActiveCartAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId && !i.IsDeleted);
        if (item is null) return CartErrors.ItemNotFound;

        if (request.Qty <= 0)
        {
            item.IsDeleted = true;
        }
        else
        {
            if (request.Qty > item.ProductVariant.Quantity) return CartErrors.InsufficientStock;
            item.Qty = request.Qty;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FinishAsync(userId, cancellationToken);
    }

    public async Task<Result<CartResponse>> RemoveItemAsync(long userId, long cartItemId, CancellationToken cancellationToken = default)
    {
        var (cart, _) = await ResolveActiveCartAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId && !i.IsDeleted);
        if (item is null) return CartErrors.ItemNotFound;

        item.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FinishAsync(userId, cancellationToken);
    }

    public async Task<Result> ClearAsync(long userId, CancellationToken cancellationToken = default)
    {
        var (cart, order) = await ResolveActiveCartAsync(userId, cancellationToken);

        foreach (var item in cart.Items.Where(i => !i.IsDeleted))
            item.IsDeleted = true;

        await ReSyncDraftOrderAsync(order, cart, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // Aktiv cartni topadi: Draft/New order bo'lsa — uning carti (tahrir order'ni re-sync qiladi),
    // aks holda open shopping cart (get-or-create).
    private async Task<(Cart cart, Order? order)> ResolveActiveCartAsync(long userId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
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

        if (order?.Cart is not null)
            return (order.Cart, order);

        var cart = await LoadOpenCartAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);
            cart = await LoadOpenCartAsync(userId, cancellationToken);
        }

        return (cart!, null);
    }

    private Task<Cart?> LoadOpenCartAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted && !c.IsCheckedOut)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Measurements)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    // Mutatsiyadan keyin: cartni qayta yuklab (yangi itemlar bilan), draft order'ni re-sync qiladi va javob qaytaradi.
    private async Task<Result<CartResponse>> FinishAsync(long userId, CancellationToken cancellationToken)
    {
        var (cart, order) = await ResolveActiveCartAsync(userId, cancellationToken);
        var pricing = await ResolvePricingContextAsync(cancellationToken);

        if (order is not null && pricing is not null)
        {
            // Yetkazib berish narxi checkout (CreateDraft/SetAddress) da qayta hisoblanadi;
            // bu yerda mavjud snapshotni saqlaymiz.
            OrderItemSync.Apply(order, cart.Items, pricing, order.DeliveryCost);
            RevertToDraftIfNew(order);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(cart, pricing);
    }

    private async Task ReSyncDraftOrderAsync(Order? order, Cart cart, CancellationToken cancellationToken)
    {
        if (order is null) return;
        var pricing = await ResolvePricingContextAsync(cancellationToken);
        if (pricing is null) return;

        OrderItemSync.Apply(order, cart.Items, pricing, order.DeliveryCost);
        RevertToDraftIfNew(order);
    }

    // To'lov boshlangan (New) order tahrirlansa — eski Hamkor summasi eskirgan → Draft'ga qaytaramiz.
    private static void RevertToDraftIfNew(Order order)
    {
        if (order.Status != OrderStatus.New) return;
        order.Status = OrderStatus.Draft;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Draft,
            Note = "Reverted to draft after cart edit",
        });
    }

    // USD rate / category rule'lar bo'lmasa savatchada raw narx qoladi (warning bilan)
    private async Task<PricingContext?> ResolvePricingContextAsync(CancellationToken cancellationToken)
    {
        var result = await pricingService.CreateContextAsync(cancellationToken);
        if (result.IsSuccess) return result.Data;

        logger.LogWarning(
            "Pricing context unavailable for cart; returning raw prices ({Error})",
            result.Error.Code);
        return null;
    }

    private static CartResponse MapToResponse(Cart cart, PricingContext? pricing)
    {
        var items = cart.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new CartItemResponse(
                i.Id,
                i.ProductVariantId,
                i.ProductVariant.ProductId,
                i.ProductVariant.Product.Name.Uz ?? i.ProductVariant.Product.Name.Ru ?? string.Empty,
                i.ProductVariant.Name,
                pricing is null
                    ? i.ProductVariant.Price
                    : pricing.CalculateSoumPrice(i.ProductVariant.Price, i.ProductVariant.Product.CategoryId),
                i.ProductVariant.Images,
                i.Qty,
                i.ProductVariant.Quantity))
            .ToList();

        var total = items.Sum(i => i.Price * i.Qty);
        return new CartResponse(cart.Id, items, total, IsLocked: false);
    }
}
