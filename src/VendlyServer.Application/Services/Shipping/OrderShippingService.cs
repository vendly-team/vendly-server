using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Diagnostics;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Shipping;

public class OrderShippingService(
    AppDbContext dbContext,
    IBtsBroker btsBroker,
    IOptions<BtsExpressOptions> options,
    ILogger<OrderShippingService> logger) : IOrderShippingService
{
    private readonly BtsExpressOptions _options = options.Value;

    public async Task<Result> ShipAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Idempotent — already shipped.
        if (!string.IsNullOrWhiteSpace(order.BtsOrderId))
            return Result.Success();

        var weight = order.Items.Where(i => !i.IsDeleted).Sum(i => (double)i.WeightKgSnap * i.Qty);
        var pieces = order.Items.Where(i => !i.IsDeleted).Sum(i => i.Qty);

        // Dropoff turi quote bilan mos bo'lishi shart: filial kodi bo'lsa filialga, bo'lmasa kuryer orqali.
        var dropoffType = string.IsNullOrWhiteSpace(order.DeliveryBtsBranchCode) ? "courier" : "branch";

        var request = new BtsCreateOrderRequest
        {
            ClientId = order.OrderNumber,
            PickupType = _options.DefaultPickupType,
            DropoffType = dropoffType,
            Sender = new BtsParty
            {
                Name = _options.SenderName,
                Phone = _options.SenderPhone,
                Address = _options.SenderAddress,
                CityCode = _options.SenderCityCode,
                BranchCode = _options.SenderBranchCode,
            },
            Receiver = new BtsParty
            {
                Name = $"{order.User.FirstName} {order.User.LastName}".Trim(),
                Phone = order.User.Phone,
                Address = $"{order.DeliveryStreet}, {order.DeliveryHouse}"
                          + (string.IsNullOrWhiteSpace(order.DeliveryExtra) ? "" : $", {order.DeliveryExtra}"),
                CityCode = order.DeliveryBtsCityCode,
                BranchCode = order.DeliveryBtsBranchCode,
            },
            Cargo = new BtsCargo
            {
                Weight = weight,
                Volume = 0, // TODO: derive from ProductMeasurement.VolumeCm3 if BTS requires it
                Piece = pieces < 1 ? 1 : pieces,
                PackageId = _options.DefaultPackageId,
                PostTypeId = _options.DefaultPostTypeId,
            },
        };

        var createResult = await btsBroker.CreateOrderAsync(request, cancellationToken);
        if (createResult.IsFailure || createResult.Data is null)
        {
            logger.LogError("Shipping: BTS create order failed for {OrderNumber}", order.OrderNumber);
            return OrderErrors.ShippingFailed;
        }

        var data = createResult.Data;
        order.BtsOrderId = data.OrderId.ToString();
        order.BtsBarcode = data.Barcode;
        // BTS ba'zan bo'sh tracking qaytaradi — bo'sh stringни saqlamaymiz.
        order.BtsTrackingUrl = string.IsNullOrWhiteSpace(data.Tracking) ? null : data.Tracking;
        order.DeliveryStatus = DeliveryStatus.Pending;

        // Sticker is best-effort — don't fail the shipment if it can't be fetched.
        var stickerResult = await btsBroker.GetStickerAsync(data.OrderId, cancellationToken);
        if (stickerResult.IsSuccess && stickerResult.Data is not null)
            order.BtsStickerUrl = stickerResult.Data.LabelSticker ?? stickerResult.Data.LabelEncode;

        return Result.Success();
    }

    public async Task<Result> CancelShipmentAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.BtsOrderId) || !long.TryParse(order.BtsOrderId, out var btsOrderId))
            return Result.Success();

        var result = await btsBroker.CancelOrderAsync(btsOrderId, cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("Shipping: BTS cancel failed for {OrderNumber} (bts {BtsOrderId})",
                order.OrderNumber, order.BtsOrderId);

        // Don't block the order cancellation on a BTS-side failure.
        return Result.Success();
    }

    public async Task<Result> ProcessWebhookAsync(BtsWebhookRequest payload, CancellationToken cancellationToken = default)
    {
        var evt = new BtsWebhookEvent
        {
            BtsOrderId = payload.OrderId,
            StatusCode = payload.StatusCode,
            StatusName = payload.StatusName ?? string.Empty,
            RawPayload = JsonSerializer.SerializeToDocument(payload),
            ReceivedAt = DateTime.UtcNow,
            IsProcessed = false,
        };
        dbContext.BtsWebhookEvents.Add(evt);

        var order = await dbContext.Orders
            .Where(o => o.BtsOrderId == payload.OrderId && !o.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            evt.Error = "Order not found for BtsOrderId";
            logger.LogWarning("BTS webhook: no order for BtsOrderId {BtsOrderId}", payload.OrderId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        order.BtsLastStatusCode = payload.StatusCode;
        order.BtsLastStatusName = payload.StatusName;
        order.BtsLastStatusAt = DateTime.UtcNow;

        var (delivery, mappedOrder) = BtsStatusMapper.Map(payload.StatusCode);
        order.DeliveryStatus = delivery;

        // Only advance the order forward and never touch a terminal order.
        if (mappedOrder is OrderStatus target
            && !OrderStatusTransitions.IsTerminal(order.Status)
            && (int)target > (int)order.Status)
        {
            order.Status = target;
            if (target == OrderStatus.Delivered)
                order.DeliveredAt = DateTime.UtcNow;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = target,
                Note = $"BTS: {payload.StatusName}",
            });
        }

        evt.IsProcessed = true;
        evt.ProcessedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
