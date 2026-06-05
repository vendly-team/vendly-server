using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Checkout.Contracts;
using VendlyServer.Infrastructure.Brokers.Hamkor;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/hamkor")]
public sealed class HamkorController(
    ICheckoutService checkoutService,
    IOptions<HamkorOptions> options,
    ILogger<HamkorController> logger) : ControllerBase
{
    private readonly HamkorOptions _options = options.Value;

    /// <summary>Payment callback (webhook) from Hamkorbank after a hosted-page payment.</summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IResult> WebhookAsync(
        [FromBody] HamkorCallbackRequest callback,
        CancellationToken cancellationToken = default)
    {
        if (!HamkorSignatureValidator.IsValid(_options.Key, _options.Secret, callback.ExtId, callback.Signature))
        {
            logger.LogWarning("Hamkor webhook: invalid signature for ext_id {ExtId}", callback.ExtId);
            return TypedResults.Unauthorized();
        }

        // Acknowledge regardless of internal outcome to avoid endless bank retries.
        await checkoutService.HandleCallbackAsync(callback, cancellationToken);
        return TypedResults.Ok();
    }
}
