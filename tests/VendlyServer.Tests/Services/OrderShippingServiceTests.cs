using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class OrderShippingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeBtsBroker _broker = new();
    private readonly OrderShippingService _service;

    public OrderShippingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        var btsOptions = Options.Create(new BtsExpressOptions
        {
            SenderName = "Vendly",
            SenderPhone = "+998900000000",
            SenderAddress = "Sender st 1",
            SenderCityCode = "TASH",
            SenderBranchCode = "SB1",
            DefaultPickupType = "branch",
            DefaultPackageId = 7,
            DefaultPostTypeId = 7,
        });

        _service = new OrderShippingService(_db, _broker, btsOptions, NullLogger<OrderShippingService>.Instance);
    }

    // ── ShipAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ship_ReturnsSuccess_AndIsIdempotent_WhenAlreadyShipped()
    {
        var order = BuildOrder();
        order.BtsOrderId = "555";

        var result = await _service.ShipAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _broker.CreateOrderCallCount);
        Assert.Equal("555", order.BtsOrderId);
    }

    [Fact]
    public async Task Ship_FillsBtsFields_OnSuccess()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData
        {
            OrderId = 9001,
            Barcode = "BC-9001",
            Tracking = "https://track/9001",
        });
        _broker.StickerResult = Result<BtsStickerData>.Success(new BtsStickerData
        {
            LabelSticker = "sticker-url",
        });

        var order = BuildOrder();

        var result = await _service.ShipAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal("9001", order.BtsOrderId);
        Assert.Equal("BC-9001", order.BtsBarcode);
        Assert.Equal("https://track/9001", order.BtsTrackingUrl);
        Assert.Equal("sticker-url", order.BtsStickerUrl);
        Assert.Equal(DeliveryStatus.Pending, order.DeliveryStatus);
    }

    [Fact]
    public async Task Ship_NullsTrackingUrl_WhenBtsReturnsBlank()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData
        {
            OrderId = 1,
            Barcode = "BC",
            Tracking = "   ",
        });

        var order = BuildOrder();

        await _service.ShipAsync(order);

        Assert.Null(order.BtsTrackingUrl);
    }

    [Fact]
    public async Task Ship_FallsBackToLabelEncode_WhenStickerHasNoSticker()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 2, Barcode = "BC", Tracking = "t" });
        _broker.StickerResult = Result<BtsStickerData>.Success(new BtsStickerData { LabelSticker = null, LabelEncode = "encoded" });

        var order = BuildOrder();

        await _service.ShipAsync(order);

        Assert.Equal("encoded", order.BtsStickerUrl);
    }

    [Fact]
    public async Task Ship_DoesNotFail_WhenStickerFetchFails()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 3, Barcode = "BC", Tracking = "t" });
        _broker.StickerResult = Result<BtsStickerData>.Failure(Error.Failure("Bts.NoSticker"));

        var order = BuildOrder();

        var result = await _service.ShipAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Null(order.BtsStickerUrl);
    }

    [Fact]
    public async Task Ship_ReturnsShippingFailed_WhenCreateFails()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Failure(Error.Failure("Bts.Boom"));

        var order = BuildOrder();

        var result = await _service.ShipAsync(order);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.ShippingFailed, result.Error);
        Assert.Null(order.BtsOrderId);
    }

    [Fact]
    public async Task Ship_ReturnsShippingFailed_WhenCreateDataNull()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(null!);

        var order = BuildOrder();

        var result = await _service.ShipAsync(order);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.ShippingFailed, result.Error);
    }

    [Fact]
    public async Task Ship_UsesCourierDropoff_WhenNoBranchCode()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 4, Barcode = "BC", Tracking = "t" });

        var order = BuildOrder();
        order.DeliveryBtsBranchCode = null;

        await _service.ShipAsync(order);

        Assert.Equal("courier", _broker.LastCreateRequest!.DropoffType);
        Assert.Equal("branch", _broker.LastCreateRequest.PickupType);
    }

    [Fact]
    public async Task Ship_UsesBranchDropoff_WhenBranchCodePresent()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 5, Barcode = "BC", Tracking = "t" });

        var order = BuildOrder();
        order.DeliveryBtsBranchCode = "BR7";

        await _service.ShipAsync(order);

        Assert.Equal("branch", _broker.LastCreateRequest!.DropoffType);
    }

    [Fact]
    public async Task Ship_AggregatesWeightAndPieces_IgnoringDeletedItems()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 6, Barcode = "BC", Tracking = "t" });

        var order = BuildOrder();
        order.Items =
        [
            new OrderItem { ProductNameSnap = "A", SkuSnap = "a", ImageSnap = "i", WeightKgSnap = 1.5m, Qty = 2 },
            new OrderItem { ProductNameSnap = "B", SkuSnap = "b", ImageSnap = "i", WeightKgSnap = 0.5m, Qty = 3 },
            new OrderItem { ProductNameSnap = "D", SkuSnap = "d", ImageSnap = "i", WeightKgSnap = 9m, Qty = 4, IsDeleted = true },
        ];

        await _service.ShipAsync(order);

        // (1.5*2) + (0.5*3) = 4.5 weight; pieces 2+3 = 5.
        Assert.Equal(4.5, _broker.LastCreateRequest!.Cargo.Weight);
        Assert.Equal(5, _broker.LastCreateRequest.Cargo.Piece);
    }

    [Fact]
    public async Task Ship_DefaultsPieceToOne_WhenNoItems()
    {
        _broker.CreateOrderResult = Result<BtsOrderData>.Success(new BtsOrderData { OrderId = 7, Barcode = "BC", Tracking = "t" });

        var order = BuildOrder();
        order.Items = [];

        await _service.ShipAsync(order);

        Assert.Equal(1, _broker.LastCreateRequest!.Cargo.Piece);
        Assert.Equal(0d, _broker.LastCreateRequest.Cargo.Weight);
    }

    // ── CancelShipmentAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ReturnsSuccess_WhenNoBtsOrderId()
    {
        var order = BuildOrder();
        order.BtsOrderId = null;

        var result = await _service.CancelShipmentAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _broker.CancelCallCount);
    }

    [Fact]
    public async Task Cancel_ReturnsSuccess_WhenBtsOrderIdNotNumeric()
    {
        var order = BuildOrder();
        order.BtsOrderId = "abc";

        var result = await _service.CancelShipmentAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _broker.CancelCallCount);
    }

    [Fact]
    public async Task Cancel_CallsBroker_WhenBtsOrderIdNumeric()
    {
        var order = BuildOrder();
        order.BtsOrderId = "8800";

        var result = await _service.CancelShipmentAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _broker.CancelCallCount);
        Assert.Equal(8800L, _broker.LastCancelledOrderId);
    }

    [Fact]
    public async Task Cancel_ReturnsSuccess_EvenWhenBrokerFails()
    {
        _broker.CancelResult = Result.Failure(Error.Failure("Bts.CancelBoom"));

        var order = BuildOrder();
        order.BtsOrderId = "8801";

        var result = await _service.CancelShipmentAsync(order);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _broker.CancelCallCount);
    }

    // ── ProcessWebhookAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWebhook_RecordsEvent_WhenOrderNotFound()
    {
        var payload = new BtsWebhookRequest { OrderId = "no-such", StatusCode = 1200, StatusName = "Delivered" };

        var result = await _service.ProcessWebhookAsync(payload);

        Assert.True(result.IsSuccess);
        var evt = _db.BtsWebhookEvents.Single();
        Assert.False(evt.IsProcessed);
        Assert.Equal("Order not found for BtsOrderId", evt.Error);
    }

    [Fact]
    public async Task ProcessWebhook_UpdatesOrder_AndAdvancesStatus()
    {
        var order = BuildOrder();
        order.BtsOrderId = "12345";
        order.Status = OrderStatus.Preparing;
        order.DeliveryStatus = DeliveryStatus.Pending;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var payload = new BtsWebhookRequest { OrderId = "12345", StatusCode = 1200, StatusName = "Delivered" };

        var result = await _service.ProcessWebhookAsync(payload);

        Assert.True(result.IsSuccess);
        var updated = _db.Orders.Single(o => o.BtsOrderId == "12345");
        Assert.Equal(OrderStatus.Delivered, updated.Status);
        Assert.Equal(DeliveryStatus.Delivered, updated.DeliveryStatus);
        Assert.NotNull(updated.DeliveredAt);
        Assert.Equal(1200, updated.BtsLastStatusCode);
        Assert.Equal("Delivered", updated.BtsLastStatusName);

        var evt = _db.BtsWebhookEvents.Single();
        Assert.True(evt.IsProcessed);
        Assert.NotNull(evt.ProcessedAt);
    }

    [Fact]
    public async Task ProcessWebhook_DoesNotAdvance_WhenOrderTerminal()
    {
        var order = BuildOrder();
        order.BtsOrderId = "777";
        order.Status = OrderStatus.Delivered;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // InTransit code; target would be earlier than terminal Delivered -> no change.
        var payload = new BtsWebhookRequest { OrderId = "777", StatusCode = 700, StatusName = "InTransit" };

        var result = await _service.ProcessWebhookAsync(payload);

        Assert.True(result.IsSuccess);
        var updated = _db.Orders.Single(o => o.BtsOrderId == "777");
        Assert.Equal(OrderStatus.Delivered, updated.Status);
        // DeliveryStatus still reflects the mapped webhook value.
        Assert.Equal(DeliveryStatus.InTransit, updated.DeliveryStatus);
    }

    [Fact]
    public async Task ProcessWebhook_DoesNotMoveBackwards()
    {
        var order = BuildOrder();
        order.BtsOrderId = "888";
        order.Status = OrderStatus.OutForDelivery;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Code 700 maps to InTransit which is < OutForDelivery -> status unchanged.
        var payload = new BtsWebhookRequest { OrderId = "888", StatusCode = 700, StatusName = "InTransit" };

        await _service.ProcessWebhookAsync(payload);

        var updated = _db.Orders.Single(o => o.BtsOrderId == "888");
        Assert.Equal(OrderStatus.OutForDelivery, updated.Status);
    }

    [Fact]
    public async Task ProcessWebhook_UnknownCode_LeavesOrderStatus_SetsDeliveryUnknown()
    {
        var order = BuildOrder();
        order.BtsOrderId = "999";
        order.Status = OrderStatus.Preparing;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var payload = new BtsWebhookRequest { OrderId = "999", StatusCode = 99999, StatusName = "??" };

        await _service.ProcessWebhookAsync(payload);

        var updated = _db.Orders.Single(o => o.BtsOrderId == "999");
        Assert.Equal(OrderStatus.Preparing, updated.Status);
        Assert.Equal(DeliveryStatus.Unknown, updated.DeliveryStatus);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Order BuildOrder() => new()
    {
        OrderNumber = "ORD-1",
        DeliveryCity = "Tashkent",
        DeliveryDistrict = "Yunusobod",
        DeliveryStreet = "Main",
        DeliveryHouse = "10",
        DeliveryBtsCityCode = "TASH",
        DeliveryBtsBranchCode = "BR1",
        User = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Phone = "+998901112233",
            PasswordHash = "x",
        },
        Items =
        [
            new OrderItem { ProductNameSnap = "P", SkuSnap = "s", ImageSnap = "i", WeightKgSnap = 1m, Qty = 1 },
        ],
    };

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
