using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Carts.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Carts;

public class CartService(AppDbContext dbContext) : ICartService
{
    public async Task<Result<CartResponse>> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(cart);
    }

    public async Task<Result<CartResponse>> AddItemAsync(long userId, CartItemRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == request.ProductVariantId && !v.IsDeleted && v.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (variant is null) return CartErrors.VariantNotFound;

        var cart = await FindCartWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync(cancellationToken);
            cart = await FindCartWithItemsAsync(userId, cancellationToken);
        }

        var existingItem = cart!.Items
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
                CartId           = cart.Id,
                ProductVariantId = request.ProductVariantId,
                Qty              = request.Qty,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await FindCartWithItemsAsync(userId, cancellationToken);
        return MapToResponse(updated!);
    }

    public async Task<Result<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .SingleOrDefaultAsync(cancellationToken);

        if (cart is null) return CartErrors.ItemNotFound;

        var item = cart.Items.SingleOrDefault(i => i.Id == cartItemId);
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
        return MapToResponse(cart);
    }

    public async Task<Result<CartResponse>> RemoveItemAsync(long userId, long cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .SingleOrDefaultAsync(cancellationToken);

        if (cart is null) return CartErrors.ItemNotFound;

        var item = cart.Items.SingleOrDefault(i => i.Id == cartItemId);
        if (item is null) return CartErrors.ItemNotFound;

        item.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(cart);
    }

    public async Task<Result> ClearAsync(long userId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.CartItems
            .Where(i => i.Cart.UserId == userId && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Cart?> FindCartWithItemsAsync(long userId, CancellationToken cancellationToken)
    {
        return await dbContext.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static CartResponse MapToResponse(Cart cart)
    {
        var items = cart.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new CartItemResponse(
                i.Id,
                i.ProductVariantId,
                i.ProductVariant.ProductId,
                i.ProductVariant.Product.Name.Uz ?? i.ProductVariant.Product.Name.Ru ?? string.Empty,
                i.ProductVariant.Name,
                i.ProductVariant.Price,
                i.ProductVariant.Images,
                i.Qty,
                i.ProductVariant.Quantity))
            .ToList();

        var total = items.Sum(i => i.Price * i.Qty);
        return new CartResponse(cart.Id, items, total);
    }
}
