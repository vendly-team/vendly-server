using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Telegram;

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

    private static TelegramWebhookHostedService CreateService(FakeTelegramBotClient client)
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
            NullLogger<TelegramWebhookHostedService>.Instance);
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

        public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((chatId, text));
            return Task.CompletedTask;
        }

        public Task AnswerInlineQueryAsync(
            string inlineQueryId,
            IReadOnlyList<Dictionary<string, object?>> results,
            int cacheTimeSeconds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
