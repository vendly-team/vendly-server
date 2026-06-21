using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Brokers.Hamkor;

namespace VendlyServer.Tests.Controllers;

public class HamkorControllerTests
{
    private readonly FakeCheckoutService _svc = new();

    private const string Key = "test-key";
    private const string Secret = "test-secret";

    private static HamkorController CreateController(ICheckoutService svc, HamkorOptions options)
    {
        return new HamkorController(svc, Options.Create(options), NullLogger<HamkorController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static HamkorOptions ValidOptions() => new() { Key = Key, Secret = Secret };

    [Fact]
    public async Task Webhook_ValidSignature_HandlesAndReturnsOk()
    {
        const string extId = "ORD-100";
        var signature = HamkorSignatureValidator.Calculate(Key, Secret, extId);
        var ctrl = CreateController(_svc, ValidOptions());

        var result = await ctrl.WebhookAsync(new HamkorCallbackRequest { ExtId = extId, State = 1, Signature = signature });

        Assert.IsType<Ok>(result);
        Assert.True(_svc.HandleCallbackCalled);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsUnauthorized()
    {
        var ctrl = CreateController(_svc, ValidOptions());

        var result = await ctrl.WebhookAsync(new HamkorCallbackRequest { ExtId = "ORD-100", State = 1, Signature = "DEADBEEF" });

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.False(_svc.HandleCallbackCalled);
    }

    [Fact]
    public async Task Webhook_MissingSignature_ReturnsUnauthorized()
    {
        var ctrl = CreateController(_svc, ValidOptions());

        var result = await ctrl.WebhookAsync(new HamkorCallbackRequest { ExtId = "ORD-100", State = 1, Signature = null });

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.False(_svc.HandleCallbackCalled);
    }

    [Fact]
    public async Task Webhook_AcksWithOk_EvenWhenServiceFails()
    {
        const string extId = "ORD-200";
        var signature = HamkorSignatureValidator.Calculate(Key, Secret, extId);
        _svc.HandleCallbackResult = Result.Failure(Error.Failure("Checkout.Failed"));
        var ctrl = CreateController(_svc, ValidOptions());

        var result = await ctrl.WebhookAsync(new HamkorCallbackRequest { ExtId = extId, State = 1, Signature = signature });

        Assert.IsType<Ok>(result);
        Assert.True(_svc.HandleCallbackCalled);
    }

    private sealed class FakeCheckoutService : ICheckoutService
    {
        public Result HandleCallbackResult { get; set; } = Result.Success();
        public bool HandleCallbackCalled { get; private set; }

        public Task<Result<CheckoutResponse>> InitiatePaymentAsync(long userId, long orderId, PaymentProvider provider, CancellationToken ct = default)
            => Task.FromResult(Result<CheckoutResponse>.Success(new("url", "ORD-1")));

        public Task<Result> HandleCallbackAsync(HamkorCallbackRequest callback, CancellationToken ct = default)
        {
            HandleCallbackCalled = true;
            return Task.FromResult(HandleCallbackResult);
        }

        public Task<Result<CheckoutStatusResponse>> GetStatusByOrderAsync(long userId, string orderNumber, CancellationToken ct = default)
            => Task.FromResult(Result<CheckoutStatusResponse>.Success(new("ORD-1", "Paid", "Confirmed")));
    }
}
