using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Telegram;
using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Tests.Services;

public class TelegramWebhookHostedServiceTests
{
    [Fact]
    public async Task StartAsync_RegistersWebhookAndNotifiesAllAdmins()
    {
        var client = new FakeTelegramBotClient();
        var service = CreateService(client);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal("https://api.vendly.uz/api/telegram/webhook", client.WebhookUrl);
        Assert.Equal("secret", client.WebhookSecret);
        Assert.Equal([10L, 20L], client.SentMessages.Select(message => message.ChatId).ToArray());
        Assert.All(client.SentMessages, message => Assert.Contains("ishga tushdi", message.Text));
    }

    [Fact]
    public async Task StopAsync_NotifiesAllAdmins()
    {
        var client = new FakeTelegramBotClient();
        var service = CreateService(client);

        await service.StopAsync(CancellationToken.None);

        Assert.Equal([10L, 20L], client.SentMessages.Select(message => message.ChatId).ToArray());
        Assert.All(client.SentMessages, message => Assert.Contains("to'xtadi", message.Text));
    }

    [Fact]
    public async Task StartAsync_PrefixesMessageWithStage_WhenEnvironmentIsStaging()
    {
        var client = new FakeTelegramBotClient();
        var service = CreateService(client, environmentName: "Staging");

        await service.StartAsync(CancellationToken.None);

        Assert.All(client.SentMessages, message => Assert.StartsWith("[STAGE] ", message.Text));
    }

    [Fact]
    public async Task StartAsync_DoesNotPrefixMessage_WhenEnvironmentIsProduction()
    {
        var client = new FakeTelegramBotClient();
        var service = CreateService(client, environmentName: "Production");

        await service.StartAsync(CancellationToken.None);

        Assert.All(client.SentMessages, message => Assert.DoesNotContain("[STAGE]", message.Text));
    }

    private static TelegramWebhookHostedService CreateService(
        FakeTelegramBotClient client,
        string environmentName = "Production")
    {
        return new TelegramWebhookHostedService(
            client,
            Options.Create(new TelegramBotOptions
            {
                Enabled = true,
                PublicBaseUrl = "https://api.vendly.uz",
                WebhookSecretToken = "secret",
                AdminChatIds = [10, 20]
            }),
            new FakeHostEnvironment { EnvironmentName = environmentName },
            NullLogger<TelegramWebhookHostedService>.Instance);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "VendlyServer.Tests";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class FakeTelegramBotClient : ITelegramBotClient
    {
        public string? WebhookUrl { get; private set; }
        public string? WebhookSecret { get; private set; }
        public List<(long ChatId, string Text)> SentMessages { get; } = [];

        public Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default)
        {
            WebhookUrl = url;
            WebhookSecret = secretToken;
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(
            long chatId,
            string text,
            CancellationToken cancellationToken = default,
            TelegramInlineKeyboardMarkup? replyMarkup = null,
            long? replyToMessageId = null)
        {
            SentMessages.Add((chatId, text));
            return Task.CompletedTask;
        }

        public Task SetMessageReactionAsync(
            long chatId,
            long messageId,
            string emoji,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AnswerInlineQueryAsync(
            string inlineQueryId,
            IReadOnlyList<Dictionary<string, object?>> results,
            int cacheTimeSeconds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
