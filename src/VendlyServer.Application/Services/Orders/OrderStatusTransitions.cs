using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Orders;

/// <summary>
/// Defines which order status transitions an admin may perform manually.
/// Forward, one step at a time; Cancel allowed from any non-terminal status.
/// (InTransit/OutForDelivery/Delivered are normally driven by the BTS webhook,
/// but admins may also advance them manually as a fallback.)
/// </summary>
public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Accepted] = [OrderStatus.Preparing],
        [OrderStatus.Preparing] = [OrderStatus.Shipped],
        [OrderStatus.Shipped] = [OrderStatus.InTransit],
        [OrderStatus.InTransit] = [OrderStatus.OutForDelivery],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
    };

    private static readonly HashSet<OrderStatus> Cancellable =
    [
        OrderStatus.Draft,
        OrderStatus.New,
        OrderStatus.Accepted,
        OrderStatus.Preparing,
        OrderStatus.Shipped,
        OrderStatus.InTransit,
        OrderStatus.OutForDelivery,
    ];

    public static bool IsAllowed(OrderStatus from, OrderStatus to)
    {
        if (to == OrderStatus.Cancelled)
            return Cancellable.Contains(from);

        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }

    public static bool IsCancellable(OrderStatus status) => Cancellable.Contains(status);

    private static readonly HashSet<OrderStatus> Terminal =
    [
        OrderStatus.Delivered,
        OrderStatus.Cancelled,
        OrderStatus.Returned,
    ];

    public static bool IsTerminal(OrderStatus status) => Terminal.Contains(status);
}
