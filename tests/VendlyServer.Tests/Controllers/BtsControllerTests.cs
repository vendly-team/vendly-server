using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Brokers.BtsExpress;

namespace VendlyServer.Tests.Controllers;

public class BtsControllerTests
{
    private readonly FakeOrderShippingService _svc = new();

    private const string HeaderName = "X-Bts-Webhook-Token";

    private static BtsController CreateController(
        IOrderShippingService svc,
        BtsExpressOptions options,
        DefaultHttpContext httpContext)
    {
        var ctrl = new BtsController(svc, Options.Create(options), NullLogger<BtsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        return ctrl;
    }

    [Fact]
    public async Task Webhook_NoSecretConfigured_AcceptsAndReturnsOk()
    {
        var ctx = new DefaultHttpContext();
        var ctrl = CreateController(_svc, new BtsExpressOptions { WebhookSecretToken = "" }, ctx);

        var result = await ctrl.WebhookAsync(new BtsWebhookRequest { OrderId = "1", StatusCode = 5 });

        Assert.IsType<Ok>(result);
        Assert.True(_svc.ProcessWebhookCalled);
    }

    [Fact]
    public async Task Webhook_SecretConfigured_ValidToken_ReturnsOk()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[HeaderName] = "s3cr3t";
        var ctrl = CreateController(_svc, new BtsExpressOptions { WebhookSecretToken = "s3cr3t" }, ctx);

        var result = await ctrl.WebhookAsync(new BtsWebhookRequest { OrderId = "1", StatusCode = 5 });

        Assert.IsType<Ok>(result);
        Assert.True(_svc.ProcessWebhookCalled);
    }

    [Fact]
    public async Task Webhook_SecretConfigured_MissingHeader_ReturnsUnauthorized()
    {
        var ctx = new DefaultHttpContext();
        var ctrl = CreateController(_svc, new BtsExpressOptions { WebhookSecretToken = "s3cr3t" }, ctx);

        var result = await ctrl.WebhookAsync(new BtsWebhookRequest { OrderId = "1", StatusCode = 5 });

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.False(_svc.ProcessWebhookCalled);
    }

    [Fact]
    public async Task Webhook_SecretConfigured_WrongToken_ReturnsUnauthorized()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[HeaderName] = "wrong";
        var ctrl = CreateController(_svc, new BtsExpressOptions { WebhookSecretToken = "s3cr3t" }, ctx);

        var result = await ctrl.WebhookAsync(new BtsWebhookRequest { OrderId = "1", StatusCode = 5 });

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.False(_svc.ProcessWebhookCalled);
    }

    [Fact]
    public async Task Webhook_AcksWithOk_EvenWhenServiceFails()
    {
        _svc.ProcessWebhookResult = Result.Failure(Error.Failure("Shipping.Failed"));
        var ctx = new DefaultHttpContext();
        var ctrl = CreateController(_svc, new BtsExpressOptions { WebhookSecretToken = "" }, ctx);

        var result = await ctrl.WebhookAsync(new BtsWebhookRequest { OrderId = "1", StatusCode = 5 });

        Assert.IsType<Ok>(result);
        Assert.True(_svc.ProcessWebhookCalled);
    }

    private sealed class FakeOrderShippingService : IOrderShippingService
    {
        public Result ProcessWebhookResult { get; set; } = Result.Success();
        public bool ProcessWebhookCalled { get; private set; }

        public Task<Result> ShipAsync(Order order, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> CancelShipmentAsync(Order order, CancellationToken ct = default) => Task.FromResult(Result.Success());

        public Task<Result> ProcessWebhookAsync(BtsWebhookRequest payload, CancellationToken ct = default)
        {
            ProcessWebhookCalled = true;
            return Task.FromResult(ProcessWebhookResult);
        }
    }
}
