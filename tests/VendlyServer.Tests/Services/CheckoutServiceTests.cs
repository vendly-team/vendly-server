using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.Hamkor;
using VendlyServer.Infrastructure.Brokers.Hamkor.Contracts;
using VendlyServer.Infrastructure.Payments;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class CheckoutServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeHamkorBroker _broker = new();
    private readonly FakePaymentProvider _payme = new("Payme");

    public CheckoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    private CheckoutService CreateService(IEnumerable<IPaymentProvider>? providers = null)
    {
        return new CheckoutService(
            _db,
            _broker,
            providers ?? new IPaymentProvider[] { _payme },
            Microsoft.Extensions.Options.Options.Create(new ClientOptions { BaseUrl = "https://shop.test/" }),
            Microsoft.Extensions.Options.Options.Create(new HamkorOptions { CallbackBaseUrl = "https://api.test/" }),
            NullLogger<CheckoutService>.Instance);
    }

    private Order SeedOrder(
        long userId = 1,
        long orderId = 1,
        string orderNumber = "ORD-1",
        OrderStatus status = OrderStatus.Draft,
        bool withItem = true,
        bool withPayment = false,
        PaymentStatus paymentStatus = PaymentStatus.Pending)
    {
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = orderNumber,
            Status = status,
            TotalAmount = 100m,
            DeliveryCost = 10m,
            DeliveryCity = "Tashkent",
            DeliveryDistrict = "D",
            DeliveryStreet = "S",
            DeliveryHouse = "1",
            DeliveryBtsCityCode = "1",
        };

        if (withItem)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = orderId,
                ProductNameSnap = "P",
                SkuSnap = "SKU",
                ImageSnap = "img.png",
                Qty = 2,
                PriceSnap = 45m,
                TotalSnap = 90m,
            });
        }

        if (withPayment)
        {
            order.Payment = new Payment
            {
                OrderId = orderId,
                Provider = PaymentProvider.Hamkor,
                Status = paymentStatus,
                Amount = order.TotalAmount,
            };
        }

        _db.Orders.Add(order);
        _db.SaveChanges();
        return order;
    }

    // ── InitiatePaymentAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Initiate_ReturnsOrderNotFound_WhenOrderMissing()
    {
        var result = await CreateService().InitiatePaymentAsync(1, 999, PaymentProvider.Payme);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Initiate_ReturnsOrderNotFound_WhenOrderBelongsToAnotherUser()
    {
        SeedOrder(userId: 1, orderId: 1);

        var result = await CreateService().InitiatePaymentAsync(userId: 2, orderId: 1, PaymentProvider.Payme);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Initiate_ReturnsNotDraft_WhenOrderNotInDraftStatus()
    {
        SeedOrder(status: OrderStatus.New);

        var result = await CreateService().InitiatePaymentAsync(1, 1, PaymentProvider.Payme);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.NotDraft, result.Error);
    }

    [Fact]
    public async Task Initiate_ReturnsProviderNotSupported_WhenNoMatchingProvider()
    {
        SeedOrder();

        var result = await CreateService(providers: Array.Empty<IPaymentProvider>())
            .InitiatePaymentAsync(1, 1, PaymentProvider.Payme);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.ProviderNotSupported, result.Error);
    }

    [Fact]
    public async Task Initiate_Succeeds_WithLocalProvider_AndPromotesOrder()
    {
        SeedOrder();
        _payme.Url = "https://pay.test/payme";

        var result = await CreateService().InitiatePaymentAsync(1, 1, PaymentProvider.Payme);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://pay.test/payme", result.Data!.PaymentUrl);
        Assert.Equal("ORD-1", result.Data.OrderNumber);

        var order = _db.Orders.Include(o => o.Payment).Single();
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.NotNull(order.Payment);
        Assert.Equal(PaymentProvider.Payme, order.Payment!.Provider);
        Assert.Equal(PaymentStatus.Pending, order.Payment.Status);
        Assert.Equal(100m, order.Payment.Amount);
    }

    [Fact]
    public async Task Initiate_Succeeds_WithHamkorBroker()
    {
        SeedOrder();
        _broker.CreateResult = Result<string>.Success("https://pay.test/hamkor");

        var result = await CreateService().InitiatePaymentAsync(1, 1, PaymentProvider.Hamkor);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://pay.test/hamkor", result.Data!.PaymentUrl);
        Assert.True(_broker.CreateCalled);

        var order = _db.Orders.Single();
        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public async Task Initiate_ReturnsPaymentUrlFailed_WhenHamkorBrokerFails()
    {
        SeedOrder();
        _broker.CreateResult = Result<string>.Failure(Error.Failure("Hamkor.Down"));

        var result = await CreateService().InitiatePaymentAsync(1, 1, PaymentProvider.Hamkor);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.PaymentUrlFailed, result.Error);

        // Order must remain in Draft when payment URL generation fails.
        Assert.Equal(OrderStatus.Draft, _db.Orders.Single().Status);
    }

    // ── HandleCallbackAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Callback_ReturnsOrderNotFound_WhenOrderMissing()
    {
        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "NOPE", State = (int)HamkorPaymentState.Confirmed });

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Callback_MarksOrderPayed_WhenStateConfirmed()
    {
        SeedOrder(status: OrderStatus.New, withPayment: true);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.Confirmed });

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Include(o => o.Payment).Single();
        Assert.Equal(OrderStatus.Payed, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.Payment!.Status);
        Assert.NotNull(order.Payment.PaidAt);
    }

    [Fact]
    public async Task Callback_MarksOrderPayed_WhenStatePayed()
    {
        SeedOrder(status: OrderStatus.New, withPayment: true);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.Payed });

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Payed, _db.Orders.Single().Status);
    }

    [Fact]
    public async Task Callback_CreatesPayment_WhenOrderHasNone()
    {
        SeedOrder(status: OrderStatus.New, withPayment: false);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.Confirmed });

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Include(o => o.Payment).Single();
        Assert.NotNull(order.Payment);
        Assert.Equal(PaymentStatus.Paid, order.Payment!.Status);
    }

    [Fact]
    public async Task Callback_MarksPaymentFailed_WhenStateCanceled()
    {
        SeedOrder(status: OrderStatus.New, withPayment: true);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.Canceled });

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Include(o => o.Payment).Single();
        Assert.Equal(PaymentStatus.Failed, order.Payment!.Status);
        // Order status is not advanced on cancel.
        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public async Task Callback_LeavesPending_WhenStateIntermediate()
    {
        SeedOrder(status: OrderStatus.New, withPayment: true);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.InProgress });

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Include(o => o.Payment).Single();
        Assert.Equal(PaymentStatus.Pending, order.Payment!.Status);
        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public async Task Callback_IsIdempotent_WhenAlreadyPaid()
    {
        SeedOrder(status: OrderStatus.Payed, withPayment: true, paymentStatus: PaymentStatus.Paid);

        var result = await CreateService().HandleCallbackAsync(
            new HamkorCallbackRequest { ExtId = "ORD-1", State = (int)HamkorPaymentState.Canceled });

        Assert.True(result.IsSuccess);
        var order = _db.Orders.Include(o => o.Payment).Single();
        // Already-paid payment is not flipped to Failed by a later callback.
        Assert.Equal(PaymentStatus.Paid, order.Payment!.Status);
    }

    // ── GetStatusByOrderAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_ReturnsOrderNotFound_WhenMissing()
    {
        var result = await CreateService().GetStatusByOrderAsync(1, "NOPE");

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task GetStatus_ReturnsOrderNotFound_WhenWrongUser()
    {
        SeedOrder(userId: 1, withPayment: true);

        var result = await CreateService().GetStatusByOrderAsync(userId: 2, "ORD-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task GetStatus_ReturnsPaymentAndOrderStatus()
    {
        SeedOrder(status: OrderStatus.Payed, withPayment: true, paymentStatus: PaymentStatus.Paid);

        var result = await CreateService().GetStatusByOrderAsync(1, "ORD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("ORD-1", result.Data!.OrderNumber);
        Assert.Equal(PaymentStatus.Paid.ToString(), result.Data.PaymentStatus);
        Assert.Equal(OrderStatus.Payed.ToString(), result.Data.OrderStatus);
    }

    [Fact]
    public async Task GetStatus_DefaultsToPending_WhenNoPayment()
    {
        SeedOrder(status: OrderStatus.Draft, withPayment: false);

        var result = await CreateService().GetStatusByOrderAsync(1, "ORD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Pending.ToString(), result.Data!.PaymentStatus);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // ── Hand fakes ────────────────────────────────────────────────────────────

    private sealed class FakeHamkorBroker : IHamkorBroker
    {
        public Result<string> CreateResult { get; set; } = Result<string>.Success("https://pay.test/hamkor");
        public bool CreateCalled { get; private set; }

        public Task<Result<string>> CreatePaymentUrlAsync(
            HamkorCreatePaymentUrlRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.FromResult(CreateResult);
        }

        public Task<Result<HamkorInvoiceResult>> GetByExtIdAsync(
            string externalId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<HamkorInvoiceResult>.Failure(Error.Failure("NotImplemented")));
    }

    private sealed class FakePaymentProvider(string name) : IPaymentProvider
    {
        public string Name { get; } = name;
        public string Url { get; set; } = "https://pay.test/local";

        public string CreatePaymentUrl(Order order) => Url;

        public Task<IResult> HandleWebhookAsync(HttpRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IResult>(Results.Ok());
    }
}
