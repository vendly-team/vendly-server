using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Telegram;
using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Tests.Controllers;

public class TelegramControllerTests
{
    [Fact]
    public async Task Webhook_ReturnsUnauthorized_WhenSecretHeaderIsInvalid()
    {
        var handler = new FakeTelegramUpdateHandler();
        var controller = CreateController(handler, "secret");
        controller.ControllerContext.HttpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "wrong";

        var result = await controller.WebhookAsync(new TelegramUpdate());

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Webhook_DispatchesUpdate_WhenSecretHeaderIsValid()
    {
        var handler = new FakeTelegramUpdateHandler();
        var controller = CreateController(handler, "secret");
        controller.ControllerContext.HttpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "secret";

        var result = await controller.WebhookAsync(new TelegramUpdate());

        Assert.IsType<Ok>(result);
        Assert.Equal(1, handler.Calls);
    }

    private static TelegramController CreateController(FakeTelegramUpdateHandler handler, string secret)
    {
        var controller = new TelegramController(
            handler,
            Options.Create(new TelegramBotOptions { WebhookSecretToken = secret }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private sealed class FakeTelegramUpdateHandler : ITelegramUpdateHandler
    {
        public int Calls { get; private set; }

        public Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
