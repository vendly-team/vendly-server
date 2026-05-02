using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramBotOptions
{
    public bool Enabled { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string WebhookSecretToken { get; set; } = string.Empty;
    public long[] AdminChatIds { get; set; } = [];
    public int InlineResultLimit { get; set; } = 5;
}

public sealed class TelegramBotOptionsSetup(IConfiguration configuration) : IConfigureOptions<TelegramBotOptions>
{
    public void Configure(TelegramBotOptions options)
    {
        configuration.GetSection("TelegramBot").Bind(options);
    }
}
