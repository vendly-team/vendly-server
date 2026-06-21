using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Payments;

namespace VendlyServer.Tests.Controllers;

public class PaymentsControllerTests
{
    private static PaymentsController CreateController(params IPaymentProvider[] providers)
    {
        var ctrl = new PaymentsController(providers);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    [Fact]
    public async Task Webhook_DispatchesToMatchingProvider()
    {
        var sentinel = Results.Ok("payme-handled");
        var payme = new FakeProvider("payme", sentinel);
        var click = new FakeProvider("click", Results.Ok("click-handled"));

        var result = await CreateController(payme, click).WebhookAsync("payme");

        Assert.Same(sentinel, result);
        Assert.True(payme.WasCalled);
        Assert.False(click.WasCalled);
    }

    [Fact]
    public async Task Webhook_IsCaseInsensitive()
    {
        var sentinel = Results.Ok("click-handled");
        var click = new FakeProvider("click", sentinel);

        var result = await CreateController(click).WebhookAsync("CLICK");

        Assert.Same(sentinel, result);
        Assert.True(click.WasCalled);
    }

    [Fact]
    public async Task Webhook_ReturnsNotFound_WhenNoProviderMatches()
    {
        var payme = new FakeProvider("payme", Results.Ok());

        var result = await CreateController(payme).WebhookAsync("unknown");

        Assert.IsType<NotFound>(result);
        Assert.False(payme.WasCalled);
    }

    [Fact]
    public async Task Webhook_ReturnsNotFound_WhenNoProvidersRegistered()
    {
        var result = await CreateController().WebhookAsync("payme");

        Assert.IsType<NotFound>(result);
    }

    private sealed class FakeProvider(string name, IResult response) : IPaymentProvider
    {
        public bool WasCalled { get; private set; }
        public string Name => name;

        public string CreatePaymentUrl(Order order) => string.Empty;

        public Task<IResult> HandleWebhookAsync(HttpRequest request, CancellationToken ct)
        {
            WasCalled = true;
            return Task.FromResult(response);
        }
    }
}
