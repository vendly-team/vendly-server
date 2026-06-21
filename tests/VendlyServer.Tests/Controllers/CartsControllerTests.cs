using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.Carts;
using VendlyServer.Application.Services.Carts.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class CartsControllerTests
{
    private readonly FakeCartService _svc = new();

    private CartsController CreateController(long userId = 1)
    {
        var ctrl = new CartsController(_svc);
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

    private static CartResponse SampleCart() => new(1, [], 0m);

    // ── GetOrCreate ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreate_ReturnsOkWithData_OnSuccess()
    {
        var cart = SampleCart();
        _svc.GetOrCreateResult = cart;

        var result = await CreateController().GetOrCreateAsync();

        var ok = Assert.IsType<Ok<CartResponse>>(result);
        Assert.Equal(cart, ok.Value);
    }

    [Fact]
    public async Task GetOrCreate_ReturnsProblem_OnFailure()
    {
        _svc.GetOrCreateError = CartErrors.CheckoutInProgress;

        var result = await CreateController().GetOrCreateAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── AddItem ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddItem_ReturnsOkWithData_OnSuccess()
    {
        _svc.AddItemResult = SampleCart();

        var result = await CreateController().AddItemAsync(new CartItemRequest(10, 2));

        Assert.IsType<Ok<CartResponse>>(result);
    }

    [Fact]
    public async Task AddItem_ReturnsProblem_OnInsufficientStock()
    {
        _svc.AddItemError = CartErrors.InsufficientStock;

        var result = await CreateController().AddItemAsync(new CartItemRequest(10, 999));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── UpdateItem ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItem_ReturnsOkWithData_OnSuccess()
    {
        _svc.UpdateItemResult = SampleCart();

        var result = await CreateController().UpdateItemAsync(1, new UpdateCartItemRequest(3));

        Assert.IsType<Ok<CartResponse>>(result);
    }

    [Fact]
    public async Task UpdateItem_ReturnsProblem_OnItemNotFound()
    {
        _svc.UpdateItemError = CartErrors.ItemNotFound;

        var result = await CreateController().UpdateItemAsync(999, new UpdateCartItemRequest(3));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── RemoveItem ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveItem_ReturnsOkWithData_OnSuccess()
    {
        _svc.RemoveItemResult = SampleCart();

        var result = await CreateController().RemoveItemAsync(1);

        Assert.IsType<Ok<CartResponse>>(result);
    }

    [Fact]
    public async Task RemoveItem_ReturnsProblem_OnItemNotFound()
    {
        _svc.RemoveItemError = CartErrors.ItemNotFound;

        var result = await CreateController().RemoveItemAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Clear ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_ReturnsOk_OnSuccess()
    {
        _svc.ClearSuccess = true;

        var result = await CreateController().ClearAsync();

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Clear_ReturnsProblem_OnFailure()
    {
        _svc.ClearError = CartErrors.CheckoutInProgress;

        var result = await CreateController().ClearAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ────────────────────────────────────────────────────────

    private class FakeCartService : ICartService
    {
        public CartResponse? GetOrCreateResult;
        public Error? GetOrCreateError;
        public CartResponse? AddItemResult;
        public Error? AddItemError;
        public CartResponse? UpdateItemResult;
        public Error? UpdateItemError;
        public CartResponse? RemoveItemResult;
        public Error? RemoveItemError;
        public bool ClearSuccess;
        public Error? ClearError;

        public Task<Result<CartResponse>> GetOrCreateAsync(long userId, CancellationToken ct = default)
            => Task.FromResult(GetOrCreateError is { } e
                ? Result<CartResponse>.Failure(e)
                : Result<CartResponse>.Success(GetOrCreateResult!));

        public Task<Result<CartResponse>> AddItemAsync(long userId, CartItemRequest request, CancellationToken ct = default)
            => Task.FromResult(AddItemError is { } e
                ? Result<CartResponse>.Failure(e)
                : Result<CartResponse>.Success(AddItemResult!));

        public Task<Result<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken ct = default)
            => Task.FromResult(UpdateItemError is { } e
                ? Result<CartResponse>.Failure(e)
                : Result<CartResponse>.Success(UpdateItemResult!));

        public Task<Result<CartResponse>> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct = default)
            => Task.FromResult(RemoveItemError is { } e
                ? Result<CartResponse>.Failure(e)
                : Result<CartResponse>.Success(RemoveItemResult!));

        public Task<Result> ClearAsync(long userId, CancellationToken ct = default)
            => Task.FromResult(ClearError is { } e ? Result.Failure(e) : Result.Success());
    }
}
