using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Admin;
using VendlyServer.Application.Services.Orders;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class AdminOrdersControllerTests
{
    private readonly FakeOrderService _svc = new();

    private AdminOrdersController CreateController(long userId = 1)
    {
        var ctrl = new AdminOrdersController(_svc);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("user_id", userId.ToString()),
                    new Claim("role", "Admin")
                ]))
            }
        };
        return ctrl;
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.ListResult = Result<List<OrderListItemResponse>>.Success([]);

        var result = await CreateController().GetAllAsync(new OrderFilterRequest());

        Assert.IsType<Ok<List<OrderListItemResponse>>>(result);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<OrderResponse>.Success(OrdersTestData.Order());

        var result = await CreateController().GetByIdAsync(1);

        Assert.IsType<Ok<OrderResponse>>(result);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdResult = Result<OrderResponse>.Failure(OrderErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── UpdateStatus ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ReturnsOkWithData_OnSuccess()
    {
        _svc.UpdateStatusResult = Result<OrderResponse>.Success(OrdersTestData.Order());

        var result = await CreateController().UpdateStatusAsync(1, new UpdateOrderStatusRequest("Preparing", null));

        Assert.IsType<Ok<OrderResponse>>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsProblem_OnInvalidTransition()
    {
        _svc.UpdateStatusResult = Result<OrderResponse>.Failure(OrderErrors.InvalidTransition);

        var result = await CreateController().UpdateStatusAsync(1, new UpdateOrderStatusRequest("Shipped", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsProblem_OnNotPaid()
    {
        _svc.UpdateStatusResult = Result<OrderResponse>.Failure(OrderErrors.NotPaid);

        var result = await CreateController().UpdateStatusAsync(1, new UpdateOrderStatusRequest("Preparing", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── AddNote ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddNote_ReturnsOkWithData_OnSuccess()
    {
        _svc.AddNoteResult = Result<OrderNoteResponse>.Success(new OrderNoteResponse(1, "n", DateTime.UtcNow));

        var result = await CreateController().AddNoteAsync(1, new AddOrderNoteRequest("n"));

        Assert.IsType<Ok<OrderNoteResponse>>(result);
    }

    [Fact]
    public async Task AddNote_ReturnsProblem_OnNotFound()
    {
        _svc.AddNoteResult = Result<OrderNoteResponse>.Failure(OrderErrors.NotFound);

        var result = await CreateController().AddNoteAsync(999, new AddOrderNoteRequest("n"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ReturnsOkWithData_OnSuccess()
    {
        _svc.CancelResult = Result<OrderResponse>.Success(OrdersTestData.Order());

        var result = await CreateController().CancelAsync(1, new CancelOrderRequest("Other", "x"));

        Assert.IsType<Ok<OrderResponse>>(result);
    }

    [Fact]
    public async Task Cancel_ReturnsProblem_OnNotCancellable()
    {
        _svc.CancelResult = Result<OrderResponse>.Failure(OrderErrors.NotCancellable);

        var result = await CreateController().CancelAsync(1, new CancelOrderRequest("Other", "x"));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetSticker ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSticker_ReturnsOk_OnSuccess()
    {
        _svc.StickerResult = Result<string>.Success("http://sticker");

        var result = await CreateController().GetStickerAsync(1);

        // Returns Results.Ok(new { sticker }) — anonymous-typed Ok<T>; assert it is not a problem.
        Assert.NotNull(result);
        Assert.IsNotType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetSticker_ReturnsProblem_OnNotAvailable()
    {
        _svc.StickerResult = Result<string>.Failure(OrderErrors.StickerNotAvailable);

        var result = await CreateController().GetStickerAsync(1);

        Assert.IsType<ProblemHttpResult>(result);
    }
}

// Shared fake + test data used by both Orders and AdminOrders controller tests.
internal sealed class FakeOrderService : IOrderService
{
    public Result<CreateOrderResponse> CreateDraftResult { get; set; } =
        Result<CreateOrderResponse>.Success(new CreateOrderResponse(1, "ORD-1"));
    public Result SetAddressResult { get; set; } = Result.Success();
    public Result<ShippingQuoteResponse> QuoteResult { get; set; } =
        Result<ShippingQuoteResponse>.Success(new ShippingQuoteResponse(7m, "Door", "UZS"));
    public Result<List<OrderListItemResponse>> ListResult { get; set; } =
        Result<List<OrderListItemResponse>>.Success([]);
    public Result CancelDraftResult { get; set; } = Result.Success();
    public Result<OrderResponse> GetMyByIdResult { get; set; } =
        Result<OrderResponse>.Success(OrdersTestData.Order());
    public Result<OrderResponse> GetByIdResult { get; set; } =
        Result<OrderResponse>.Success(OrdersTestData.Order());
    public Result<OrderResponse> UpdateStatusResult { get; set; } =
        Result<OrderResponse>.Success(OrdersTestData.Order());
    public Result<OrderNoteResponse> AddNoteResult { get; set; } =
        Result<OrderNoteResponse>.Success(new OrderNoteResponse(1, "n", DateTime.UtcNow));
    public Result<OrderResponse> CancelResult { get; set; } =
        Result<OrderResponse>.Success(OrdersTestData.Order());
    public Result<string> StickerResult { get; set; } = Result<string>.Success("http://sticker");

    public Task<Result<CreateOrderResponse>> CreateDraftAsync(long userId, CreateOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(CreateDraftResult);
    public Task<Result> SetAddressAsync(long userId, long id, SetOrderAddressRequest request, CancellationToken ct = default)
        => Task.FromResult(SetAddressResult);
    public Task<Result<ShippingQuoteResponse>> QuoteForAddressAsync(long userId, long addressId, CancellationToken ct = default)
        => Task.FromResult(QuoteResult);
    public Task<Result<List<OrderListItemResponse>>> GetActiveOrdersAsync(long userId, CancellationToken ct = default)
        => Task.FromResult(ListResult);
    public Task<Result> CancelDraftAsync(long userId, long orderId, CancellationToken ct = default)
        => Task.FromResult(CancelDraftResult);
    public Task<Result<List<OrderListItemResponse>>> GetMyOrdersAsync(long userId, OrderFilterRequest filter, CancellationToken ct = default)
        => Task.FromResult(ListResult);
    public Task<Result<OrderResponse>> GetMyByIdAsync(long userId, long id, CancellationToken ct = default)
        => Task.FromResult(GetMyByIdResult);
    public Task<Result<List<OrderListItemResponse>>> GetAllAsync(OrderFilterRequest filter, CancellationToken ct = default)
        => Task.FromResult(ListResult);
    public Task<Result<OrderResponse>> GetByIdAsync(long id, CancellationToken ct = default)
        => Task.FromResult(GetByIdResult);
    public Task<Result<OrderResponse>> UpdateStatusAsync(long adminId, long id, UpdateOrderStatusRequest request, CancellationToken ct = default)
        => Task.FromResult(UpdateStatusResult);
    public Task<Result<OrderNoteResponse>> AddNoteAsync(long adminId, long id, AddOrderNoteRequest request, CancellationToken ct = default)
        => Task.FromResult(AddNoteResult);
    public Task<Result<OrderResponse>> CancelAsync(long adminId, string role, long id, CancelOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(CancelResult);
    public Task<Result<string>> GetStickerAsync(long id, CancellationToken ct = default)
        => Task.FromResult(StickerResult);
}

internal static class OrdersTestData
{
    public static OrderResponse Order() => new(
        1, "ORD-1", "Draft", "Pending", "Pending",
        100m, 7m, 0m, 107m,
        new OrderDeliveryResponse("Tashkent", "Yunusobod", "Amir Temur", "1", null, "TAS", null, null, null, null, null),
        null, [], [], [], null, DateTime.UtcNow);
}
