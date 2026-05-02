using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Application.Services.Telegram;

public interface ITelegramUpdateHandler
{
    Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken = default);
}
