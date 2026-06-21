using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Tests.Controllers;

public class CheckoutControllerTests
{
    private readonly FakeCheckoutService _svc = new();

    private CheckoutController CreateController(long userId = 1)
    {
        var ctrl = new CheckoutController(_svc);
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

    [Fact]
    public async Task GetStatus_ReturnsOkWithData_OnSuccess()
    {
        var status = new CheckoutStatusResponse("ORD-1", "Paid", "Payed");
        _svc.GetStatusResult = Result<CheckoutStatusResponse>.Success(status);

        var result = await CreateController().GetStatusAsync("ORD-1");

        var ok = Assert.IsType<Ok<CheckoutStatusResponse>>(result);
        Assert.Equal(status, ok.Value);
    }

    [Fact]
    public async Task GetStatus_PassesUserIdFromClaim()
    {
        _svc.GetStatusResult = Result<CheckoutStatusResponse>.Success(
            new CheckoutStatusResponse("ORD-1", "Pending", "New"));

        await CreateController(userId: 42).GetStatusAsync("ORD-1");

        Assert.Equal(42, _svc.LastUserId);
        Assert.Equal("ORD-1", _svc.LastOrderNumber);
    }

    [Fact]
    public async Task GetStatus_ReturnsProblem_OnNotFound()
    {
        _svc.GetStatusResult = Result<CheckoutStatusResponse>.Failure(CheckoutErrors.OrderNotFound);

        var result = await CreateController().GetStatusAsync("NOPE");

        Assert.IsType<ProblemHttpResult>(result);
    }

    private sealed class FakeCheckoutService : ICheckoutService
    {
        public Result<CheckoutStatusResponse> GetStatusResult { get; set; } =
            Result<CheckoutStatusResponse>.Success(new CheckoutStatusResponse("ORD-1", "Pending", "New"));

        public long LastUserId { get; private set; }
        public string? LastOrderNumber { get; private set; }

        public Task<Result<CheckoutResponse>> InitiatePaymentAsync(
            long userId, long orderId, PaymentProvider provider, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<CheckoutResponse>.Success(new CheckoutResponse("url", "ORD-1")));

        public Task<Result> HandleCallbackAsync(
            HamkorCallbackRequest callback, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());

        public Task<Result<CheckoutStatusResponse>> GetStatusByOrderAsync(
            long userId, string orderNumber, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastOrderNumber = orderNumber;
            return Task.FromResult(GetStatusResult);
        }
    }
}
