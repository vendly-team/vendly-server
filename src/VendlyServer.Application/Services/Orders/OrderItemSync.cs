using VendlyServer.Application.Services.Pricing;
using VendlyServer.Domain.Entities.Orders;

namespace VendlyServer.Application.Services.Orders;

// Order item snapshotlarini cart bilan moslaydi: eski itemlarni soft-delete qiladi,
// cartdagi itemlardan qayta quradi (so'm narx bilan) va Subtotal/TotalAmount ni qayta hisoblaydi.
// Create va cart-edit (re-sync) da bir xil ishlatiladi.
public static class OrderItemSync
{
    public static void Apply(Order order, IEnumerable<CartItem> cartItems, PricingContext pricing, decimal deliveryCost)
    {
        foreach (var existing in order.Items.Where(i => !i.IsDeleted))
            existing.IsDeleted = true;

        decimal subtotal = 0;

        foreach (var item in cartItems.Where(i => !i.IsDeleted))
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

            subtotal += unitPrice * item.Qty;
        }

        order.Subtotal = subtotal;
        order.DeliveryCost = deliveryCost;
        order.TotalAmount = subtotal + deliveryCost;
    }
}
