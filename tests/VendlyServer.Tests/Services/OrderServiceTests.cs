using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeShipping _shipping = new();
    private readonly FakeCalculator _calculator = new();
    private readonly OrderService _service;

    private const long UserId = 1;
    private const long AddressId = 1;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new OrderService(_db, _shipping, _calculator, new StubPricingService());

        _db.Users.Add(new User
        {
            Id = UserId, FirstName = "Ali", LastName = "Valiyev",
            Phone = "+998900000000", PasswordHash = "x"
        });
        _db.Addresses.Add(new Address
        {
            Id = AddressId, UserId = UserId, Label = "Home", City = "Tashkent",
            District = "Yunusobod", Street = "Amir Temur", House = "1",
            BtsCityCode = "TAS", BtsBranchCode = "B1"
        });

        var category = new Category { Id = 1, Name = "Cat" };
        var product = new Product { Id = 1, CategoryId = 1, Name = new MultiLanguageField { Uz = "Prod" } };
        var variant = new ProductVariant
        {
            Id = 1, ProductId = 1, Quantity = 10, IsActive = true, Price = 50m, Name = "Var1"
        };
        _db.Categories.Add(category);
        _db.Products.Add(product);
        _db.ProductVariants.Add(variant);
        _db.ProductMeasurements.Add(new ProductMeasurement { Id = 1, ProductVariantId = 1, WeightKg = 2m });
        _db.SaveChanges();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Cart> SeedOpenCartWithItem(int qty = 2)
    {
        var cart = new Cart { UserId = UserId, IsCheckedOut = false };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        _db.CartItems.Add(new CartItem { CartId = cart.Id, ProductVariantId = 1, Qty = qty });
        await _db.SaveChangesAsync();
        return cart;
    }

    private Order NewOrder(OrderStatus status, long userId = UserId) => new()
    {
        UserId = userId, OrderNumber = "ORD-1", Status = status,
        DeliveryCity = "Tashkent", DeliveryDistrict = "Yunusobod",
        DeliveryStreet = "Amir Temur", DeliveryHouse = "1", DeliveryBtsCityCode = "TAS"
    };

    // ── CreateDraftAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraft_ReturnsAddressNotFound_WhenAddressMissing()
    {
        var result = await _service.CreateDraftAsync(UserId, new CreateOrderRequest(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.AddressNotFound, result.Error);
    }

    [Fact]
    public async Task CreateDraft_ReturnsCartEmpty_WhenNoOpenCart()
    {
        var result = await _service.CreateDraftAsync(UserId, new CreateOrderRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.CartEmpty, result.Error);
    }

    [Fact]
    public async Task CreateDraft_CreatesDraftOrder_AndMarksCartCheckedOut()
    {
        var cart = await SeedOpenCartWithItem(qty: 2);

        var result = await _service.CreateDraftAsync(UserId, new CreateOrderRequest(AddressId));

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Single();
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(cart.Id, order.CartId);
        Assert.True(_db.Carts.Single(c => c.Id == cart.Id).IsCheckedOut);
        // 2 qty * 50 price = 100 subtotal; delivery cost from calculator = 7
        Assert.Equal(100m, order.Subtotal);
        Assert.Equal(7m, order.DeliveryCost);
        Assert.Equal(107m, order.TotalAmount);
    }

    [Fact]
    public async Task CreateDraft_ReturnsWeightMissing_WhenVariantHasNoWeight()
    {
        _db.ProductMeasurements.Single(m => m.ProductVariantId == 1).WeightKg = 0m;
        await _db.SaveChangesAsync();
        await SeedOpenCartWithItem();

        var result = await _service.CreateDraftAsync(UserId, new CreateOrderRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.WeightMissing, result.Error);
    }

    [Fact]
    public async Task CreateDraft_RevertsNewOrderToDraft_OnResume()
    {
        var cart = await SeedOpenCartWithItem();
        // Existing New order tied to that cart.
        var order = NewOrder(OrderStatus.New);
        order.CartId = cart.Id;
        _db.Orders.Add(order);
        cart.IsCheckedOut = true;
        await _db.SaveChangesAsync();

        var result = await _service.CreateDraftAsync(UserId, new CreateOrderRequest(AddressId));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Draft, _db.Orders.Single().Status);
    }

    [Fact]
    public async Task CreateDraft_ReturnsPricingError_WhenPricingFails()
    {
        var failing = new OrderService(_db, _shipping, _calculator, new FailingPricingService());
        await SeedOpenCartWithItem();

        var result = await failing.CreateDraftAsync(UserId, new CreateOrderRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(FailingPricingService.Err, result.Error);
    }

    // ── QuoteForAddressAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task QuoteForAddress_ReturnsAddressNotFound_WhenMissing()
    {
        var result = await _service.QuoteForAddressAsync(UserId, 999);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.AddressNotFound, result.Error);
    }

    [Fact]
    public async Task QuoteForAddress_ReturnsCartEmpty_WhenNoCart()
    {
        var result = await _service.QuoteForAddressAsync(UserId, AddressId);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.CartEmpty, result.Error);
    }

    [Fact]
    public async Task QuoteForAddress_ReturnsQuote_OnSuccess()
    {
        await SeedOpenCartWithItem();

        var result = await _service.QuoteForAddressAsync(UserId, AddressId);

        Assert.True(result.IsSuccess);
        Assert.Equal(7m, result.Data!.Cost);
    }

    // ── SetAddressAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetAddress_ReturnsAddressNotFound_WhenMissing()
    {
        var result = await _service.SetAddressAsync(UserId, 1, new SetOrderAddressRequest(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.AddressNotFound, result.Error);
    }

    [Fact]
    public async Task SetAddress_ReturnsNotFound_WhenOrderMissing()
    {
        var result = await _service.SetAddressAsync(UserId, 999, new SetOrderAddressRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task SetAddress_ReturnsNotDraft_WhenOrderNotDraft()
    {
        var order = NewOrder(OrderStatus.Payed);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.SetAddressAsync(UserId, order.Id, new SetOrderAddressRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotDraft, result.Error);
    }

    [Fact]
    public async Task SetAddress_ReturnsWeightMissing_WhenItemWeightZero()
    {
        var order = NewOrder(OrderStatus.Draft);
        order.Items.Add(new OrderItem
        {
            ProductNameSnap = "p", SkuSnap = "s", ImageSnap = "", WeightKgSnap = 0m, Qty = 1, PriceSnap = 10m, TotalSnap = 10m
        });
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.SetAddressAsync(UserId, order.Id, new SetOrderAddressRequest(AddressId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.WeightMissing, result.Error);
    }

    [Fact]
    public async Task SetAddress_UpdatesCostAndTotal_OnSuccess()
    {
        var order = NewOrder(OrderStatus.Draft);
        order.Subtotal = 100m;
        order.Items.Add(new OrderItem
        {
            ProductNameSnap = "p", SkuSnap = "s", ImageSnap = "", WeightKgSnap = 2m, Qty = 1, PriceSnap = 100m, TotalSnap = 100m
        });
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.SetAddressAsync(UserId, order.Id, new SetOrderAddressRequest(AddressId));

        Assert.True(result.IsSuccess);
        var updated = _db.Orders.Single(o => o.Id == order.Id);
        Assert.Equal(7m, updated.DeliveryCost);
        Assert.Equal(107m, updated.TotalAmount);
    }

    // ── CancelDraftAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDraft_ReturnsNotFound_WhenNoDraftOrNew()
    {
        var order = NewOrder(OrderStatus.Payed);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.CancelDraftAsync(UserId, order.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task CancelDraft_SetsCancelled_OnSuccess()
    {
        var order = NewOrder(OrderStatus.Draft);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.CancelDraftAsync(UserId, order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, _db.Orders.Single(o => o.Id == order.Id).Status);
    }

    // ── GetActiveOrdersAsync / GetMyOrdersAsync / GetMyByIdAsync ───────────────

    [Fact]
    public async Task GetActiveOrders_ReturnsOnlyActiveStatuses()
    {
        _db.Orders.Add(NewOrder(OrderStatus.Preparing));
        _db.Orders.Add(NewOrder(OrderStatus.Delivered));
        await _db.SaveChangesAsync();

        var result = await _service.GetActiveOrdersAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetMyOrders_FiltersByStatus()
    {
        _db.Orders.Add(NewOrder(OrderStatus.Delivered));
        _db.Orders.Add(NewOrder(OrderStatus.Preparing));
        await _db.SaveChangesAsync();

        var result = await _service.GetMyOrdersAsync(UserId, new OrderFilterRequest { Status = "Delivered" });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Delivered", result.Data![0].Status);
    }

    [Fact]
    public async Task GetMyById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetMyByIdAsync(UserId, 999);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetMyById_ReturnsOrder_OnSuccess()
    {
        var order = NewOrder(OrderStatus.Preparing);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.GetMyByIdAsync(UserId, order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Data!.Id);
    }

    // ── Admin: GetAllAsync / GetByIdAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAll_FiltersBySearchTerm()
    {
        var o = NewOrder(OrderStatus.Preparing);
        o.OrderNumber = "ORD-FINDME";
        _db.Orders.Add(o);
        _db.Orders.Add(NewOrder(OrderStatus.Preparing));
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(new OrderFilterRequest { Search = "FINDME" });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ReturnsUnknownStatus_WhenUnparseable()
    {
        var result = await _service.UpdateStatusAsync(2, 1, new UpdateOrderStatusRequest("Bogus", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.UnknownStatus, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsInvalidTransition_WhenTargetCancelled()
    {
        var result = await _service.UpdateStatusAsync(2, 1, new UpdateOrderStatusRequest("Cancelled", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.InvalidTransition, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenOrderMissing()
    {
        var result = await _service.UpdateStatusAsync(2, 999, new UpdateOrderStatusRequest("Preparing", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotPaid_WhenNoPaidPayment()
    {
        var order = NewOrder(OrderStatus.Payed);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.UpdateStatusAsync(2, order.Id, new UpdateOrderStatusRequest("Preparing", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotPaid, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsInvalidTransition_WhenNotAllowed()
    {
        var order = NewOrder(OrderStatus.Payed);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _db.Payments.Add(new Payment { OrderId = order.Id, Status = PaymentStatus.Paid, Amount = 1m });
        await _db.SaveChangesAsync();

        // Payed -> Shipped is not a single allowed step (only Payed -> Preparing).
        var result = await _service.UpdateStatusAsync(2, order.Id, new UpdateOrderStatusRequest("Shipped", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.InvalidTransition, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_Advances_OnSuccess()
    {
        var order = NewOrder(OrderStatus.Payed);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _db.Payments.Add(new Payment { OrderId = order.Id, Status = PaymentStatus.Paid, Amount = 1m });
        await _db.SaveChangesAsync();

        var result = await _service.UpdateStatusAsync(2, order.Id, new UpdateOrderStatusRequest("Preparing", "go"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Preparing", result.Data!.Status);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsShippingFailed_WhenShipFails()
    {
        var order = NewOrder(OrderStatus.Preparing);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _db.Payments.Add(new Payment { OrderId = order.Id, Status = PaymentStatus.Paid, Amount = 1m });
        await _db.SaveChangesAsync();
        _shipping.ShipResult = Result.Failure(ShippingErrors.CalculateFailed);

        var result = await _service.UpdateStatusAsync(2, order.Id, new UpdateOrderStatusRequest("Shipped", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.ShippingFailed, result.Error);
    }

    [Fact]
    public async Task UpdateStatus_CallsShip_WhenTargetShipped()
    {
        var order = NewOrder(OrderStatus.Preparing);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _db.Payments.Add(new Payment { OrderId = order.Id, Status = PaymentStatus.Paid, Amount = 1m });
        await _db.SaveChangesAsync();

        var result = await _service.UpdateStatusAsync(2, order.Id, new UpdateOrderStatusRequest("Shipped", null));

        Assert.True(result.IsSuccess);
        Assert.True(_shipping.ShipCalled);
        Assert.Equal("Shipped", result.Data!.Status);
    }

    // ── AddNoteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddNote_ReturnsNotFound_WhenOrderMissing()
    {
        var result = await _service.AddNoteAsync(2, 999, new AddOrderNoteRequest("hi"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task AddNote_PersistsNote_OnSuccess()
    {
        var order = NewOrder(OrderStatus.Preparing);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.AddNoteAsync(2, order.Id, new AddOrderNoteRequest("hello"));

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Data!.Note);
        Assert.Single(_db.OrderNotes);
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenOrderMissing()
    {
        var result = await _service.CancelAsync(2, "Admin", 999, new CancelOrderRequest("Other", "x"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Cancel_ReturnsNotCancellable_WhenTerminal()
    {
        var order = NewOrder(OrderStatus.Delivered);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.CancelAsync(2, "Admin", order.Id, new CancelOrderRequest("Other", "x"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotCancellable, result.Error);
    }

    [Fact]
    public async Task Cancel_SetsCancelled_AndRecordsCancellation()
    {
        var order = NewOrder(OrderStatus.Preparing);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.CancelAsync(2, "Admin", order.Id, new CancelOrderRequest("Other", "reason"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Data!.Status);
        Assert.Single(_db.OrderCancellations);
    }

    [Fact]
    public async Task Cancel_CancelsBtsShipment_WhenBtsOrderIdPresent()
    {
        var order = NewOrder(OrderStatus.Shipped);
        order.BtsOrderId = "BTS-99";
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.CancelAsync(2, "Admin", order.Id, new CancelOrderRequest(null, null));

        Assert.True(result.IsSuccess);
        Assert.True(_shipping.CancelCalled);
    }

    // ── GetStickerAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSticker_ReturnsNotFound_WhenOrderMissing()
    {
        var result = await _service.GetStickerAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetSticker_ReturnsStickerNotAvailable_WhenEmpty()
    {
        var order = NewOrder(OrderStatus.Shipped);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.GetStickerAsync(order.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderErrors.StickerNotAvailable, result.Error);
    }

    [Fact]
    public async Task GetSticker_ReturnsUrl_WhenPresent()
    {
        var order = NewOrder(OrderStatus.Shipped);
        order.BtsStickerUrl = "http://sticker";
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.GetStickerAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("http://sticker", result.Data);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeCalculator : IShippingCalculatorService
    {
        public Result<ShippingQuoteResponse> Result { get; set; } =
            Domain.Abstractions.Result<ShippingQuoteResponse>.Success(new ShippingQuoteResponse(7m, "Door", "UZS"));

        public Task<Result<ShippingQuoteResponse>> CalculateAsync(ShippingQuoteRequest request, CancellationToken ct = default)
            => Task.FromResult(Result);
    }

    private sealed class FakeShipping : IOrderShippingService
    {
        public Result ShipResult { get; set; } = Result.Success();
        public bool ShipCalled { get; private set; }
        public bool CancelCalled { get; private set; }

        public Task<Result> ShipAsync(Order order, CancellationToken ct = default)
        {
            ShipCalled = true;
            return Task.FromResult(ShipResult);
        }

        public Task<Result> CancelShipmentAsync(Order order, CancellationToken ct = default)
        {
            CancelCalled = true;
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ProcessWebhookAsync(BtsWebhookRequest payload, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }

    private sealed class FailingPricingService : Application.Services.Pricing.IProductPricingService
    {
        public static readonly Error Err = Error.Failure("Pricing.Unavailable");

        public Task<Result<Application.Services.Pricing.PricingContext>> CreateContextAsync(CancellationToken ct = default)
            => Task.FromResult(Result<Application.Services.Pricing.PricingContext>.Failure(Err));
    }
}
