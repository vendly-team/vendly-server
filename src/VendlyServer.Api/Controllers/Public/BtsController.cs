using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Infrastructure.Brokers.BtsExpress;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/bts")]
public sealed class BtsController(
    IOrderShippingService shippingService,
    IOptions<BtsExpressOptions> options,
    ILogger<BtsController> logger) : ControllerBase
{
    private const string SecretHeaderName = "X-Bts-Webhook-Token";
    private readonly BtsExpressOptions _options = options.Value;

    /// <summary>Delivery status webhook from BTS Express.</summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IResult> WebhookAsync(
        [FromBody] BtsWebhookRequest payload,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidSecret())
        {
            logger.LogWarning("BTS webhook: invalid secret token");
            return TypedResults.Unauthorized();
        }

        // Acknowledge regardless of internal outcome to avoid endless retries.
        await shippingService.ProcessWebhookAsync(payload, cancellationToken);
        return TypedResults.Ok();
    }

    private bool IsValidSecret()
    {
        // If no secret is configured, accept all (e.g. local/dev).
        if (string.IsNullOrWhiteSpace(_options.WebhookSecretToken))
            return true;

        return Request.Headers.TryGetValue(SecretHeaderName, out var token) &&
               string.Equals(token.ToString(), _options.WebhookSecretToken, StringComparison.Ordinal);
    }
}
