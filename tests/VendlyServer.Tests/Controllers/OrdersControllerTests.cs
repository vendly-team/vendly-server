using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Orders;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly FakeOrderService _orders = new();
    private readonly FakeCheckoutService _checkout = new();

    private OrdersController CreateController(long userId = 1)
    {
        var ctrl = new OrdersController(_orders, _checkout);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("user_id", userId.ToString())]))
            }
        };
        return ctrl;
    }

    // ── CreateDraft ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraft_ReturnsOkWithData_OnSuccess()
    {
        _orders.CreateDraftResult = Result<CreateOrderResponse>.Success(new CreateOrderResponse(1, "ORD-1"));

        var result = await CreateController().CreateDraftAsync(new CreateOrderRequest(1));

        var ok = Assert.IsType<Ok<CreateOrderResponse>>(result);
        Assert.Equal("ORD-1", ok.Value!.OrderNumber);
    }

    [Fact]
    public async Task CreateDraft_ReturnsProblem_OnFailure()
    {
        _orders.CreateDraftResult = Result<CreateOrderResponse>.Failure(OrderErrors.CartEmpty);

        var result = await CreateController().CreateDraftAsync(new CreateOrderRequest(1));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── SetAddress ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAddress_ReturnsOk_OnSuccess()
    {
        _orders.SetAddressResult = Result.Success();

        var result = await CreateController().SetAddressAsync(1, new SetOrderAddressRequest(1));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task SetAddress_ReturnsProblem_OnNotDraft()
    {
        _orders.SetAddressResult = Result.Failure(OrderErrors.NotDraft);

        var result = await CreateController().SetAddressAsync(1, new SetOrderAddressRequest(1));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── ShippingQuote ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShippingQuote_ReturnsOkWithData_OnSuccess()
    {
        _orders.QuoteResult = Result<ShippingQuoteResponse>.Success(new ShippingQuoteResponse(7m, "Door", "UZS"));

        var result = await CreateController().GetShippingQuoteAsync(1);

        var ok = Assert.IsType<Ok<ShippingQuoteResponse>>(result);
        Assert.Equal(7m, ok.Value!.Cost);
    }

    [Fact]
    public async Task GetShippingQuote_ReturnsProblem_OnFailure()
    {
        _orders.QuoteResult = Result<ShippingQuoteResponse>.Failure(OrderErrors.AddressNotFound);

        var result = await CreateController().GetShippingQuoteAsync(1);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── InitiatePayment (checkout) ────────────────────────────────────────────

    [Fact]
    public async Task InitiatePayment_ReturnsOkWithData_OnSuccess()
    {
        _checkout.InitiateResult = Result<CheckoutResponse>.Success(new CheckoutResponse("http://pay", "ORD-1"));

        var result = await CreateController().InitiatePaymentAsync(1, new InitiatePaymentRequest { Provider = PaymentProvider.Hamkor });

        var ok = Assert.IsType<Ok<CheckoutResponse>>(result);
        Assert.Equal("http://pay", ok.Value!.PaymentUrl);
    }

    [Fact]
    public async Task InitiatePayment_ReturnsProblem_OnFailure()
    {
        _checkout.InitiateResult = Result<CheckoutResponse>.Failure(OrderErrors.NotDraft);

        var result = await CreateController().InitiatePaymentAsync(1, new InitiatePaymentRequest { Provider = PaymentProvider.Hamkor });

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── CancelDraft ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDraft_ReturnsOk_OnSuccess()
    {
        _orders.CancelDraftResult = Result.Success();

        var result = await CreateController().CancelDraftAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task CancelDraft_ReturnsProblem_OnNotFound()
    {
        _orders.CancelDraftResult = Result.Failure(OrderErrors.NotFound);

        var result = await CreateController().CancelDraftAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetActive ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOkWithData_OnSuccess()
    {
        _orders.ListResult = Result<List<OrderListItemResponse>>.Success([]);

        var result = await CreateController().GetActiveAsync();

        Assert.IsType<Ok<List<OrderListItemResponse>>>(result);
    }

    // ── GetMy ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMy_ReturnsOkWithData_OnSuccess()
    {
        _orders.ListResult = Result<List<OrderListItemResponse>>.Success([]);

        var result = await CreateController().GetMyAsync(new OrderFilterRequest());

        Assert.IsType<Ok<List<OrderListItemResponse>>>(result);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _orders.GetMyByIdResult = Result<OrderResponse>.Success(OrdersTestData.Order());

        var result = await CreateController().GetByIdAsync(1);

        Assert.IsType<Ok<OrderResponse>>(result);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _orders.GetMyByIdResult = Result<OrderResponse>.Failure(OrderErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeCheckoutService : ICheckoutService
    {
        public Result<CheckoutResponse> InitiateResult { get; set; } =
            Result<CheckoutResponse>.Success(new CheckoutResponse("http://pay", "ORD-1"));

        public Task<Result<CheckoutResponse>> InitiatePaymentAsync(long userId, long orderId, PaymentProvider provider, CancellationToken ct = default)
            => Task.FromResult(InitiateResult);

        public Task<Result> HandleCallbackAsync(HamkorCallbackRequest callback, CancellationToken ct = default)
            => Task.FromResult(Result.Success());

        public Task<Result<CheckoutStatusResponse>> GetStatusByOrderAsync(long userId, string orderNumber, CancellationToken ct = default)
            => Task.FromResult(Result<CheckoutStatusResponse>.Success(new CheckoutStatusResponse(orderNumber, "Pending", "Draft")));
    }
}
