using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using VendlyServer.Application.Services.Telegram;
using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/telegram")]
public sealed class TelegramController(
    ITelegramUpdateHandler updateHandler,
    IOptions<TelegramBotOptions> options) : ControllerBase
{
    private const string SecretTokenHeaderName = "X-Telegram-Bot-Api-Secret-Token";
    private readonly TelegramBotOptions _options = options.Value;

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IResult> WebhookAsync(
        [FromBody] TelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidSecretToken())
            return TypedResults.Unauthorized();

        await updateHandler.HandleAsync(update, cancellationToken);
        return TypedResults.Ok();
    }

    private bool IsValidSecretToken()
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecretToken))
            return false;

        return Request.Headers.TryGetValue(SecretTokenHeaderName, out var actualSecretToken) &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(actualSecretToken.ToString()),
                   Encoding.UTF8.GetBytes(_options.WebhookSecretToken));
    }
}
