using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramWebhookHostedService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramWebhookHostedService> logger) : IHostedService
{
    private readonly TelegramBotOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!CanRun())
            return;

        var webhookUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/telegram/webhook";
        await botClient.SetWebhookAsync(webhookUrl, _options.WebhookSecretToken, cancellationToken);
        await NotifyAdminsAsync("✅ Vendly Telegram bot ishga tushdi. Inline qidiruv tayyor 🔎", cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!CanRun())
            return;

        await NotifyAdminsAsync("🛑 Vendly Telegram bot to'xtadi.", cancellationToken);
    }

    private bool CanRun()
    {
        if (!_options.Enabled)
            return false;

        var canRun = !string.IsNullOrWhiteSpace(_options.PublicBaseUrl) &&
                     !string.IsNullOrWhiteSpace(_options.WebhookSecretToken);

        if (!canRun)
            logger.LogInformation("Telegram bot is enabled but configuration is incomplete.");

        return canRun;
    }

    private async Task NotifyAdminsAsync(string text, CancellationToken cancellationToken)
    {
        foreach (var adminChatId in _options.AdminChatIds)
        {
            try
            {
                await botClient.SendMessageAsync(adminChatId, text, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Failed to send Telegram lifecycle notification to admin chat {AdminChatId}.",
                    adminChatId);
            }
        }
    }
}
