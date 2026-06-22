using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Application.Services.Shipping.Contracts;

namespace VendlyServer.Application.Services.Shipping;

public interface IOrderShippingService
{
    /// <summary>
    /// Creates a BTS delivery for the order and fills its BTS fields (id, barcode, tracking, sticker).
    /// Mutates the tracked <paramref name="order"/>; the caller persists via SaveChanges.
    /// Requires the order's Items and User to be loaded.
    /// </summary>
    Task<Result> ShipAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Cancels the BTS delivery for the order (if one exists). Mutates the tracked order.</summary>
    Task<Result> CancelShipmentAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Processes a BTS status webhook: records the event and updates the matching order.</summary>
    Task<Result> ProcessWebhookAsync(BtsWebhookRequest payload, CancellationToken cancellationToken = default);
}
