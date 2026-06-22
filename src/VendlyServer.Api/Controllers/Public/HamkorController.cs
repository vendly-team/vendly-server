using System.Text.Json;
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
    private const string LogScope = "Hamkor";

    private readonly HamkorOptions _options = options.Value;

    /// <summary>Payment callback (webhook) from Hamkorbank after a hosted-page payment.</summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IResult> WebhookAsync(
        [FromBody] HamkorCallbackRequest callback,
        CancellationToken cancellationToken = default)
    {
        var bodyJson = JsonSerializer.Serialize(callback);

        logger.LogInformation(
            "{Scope} ← webhook received ext_id={ExtId} state={State} Body: {Body}",
            LogScope, callback.ExtId, callback.State, bodyJson);

        if (!HamkorSignatureValidator.IsValid(_options.Key, _options.Secret, callback.ExtId, callback.Signature))
        {
            logger.LogWarning(
                "{Scope} ← webhook signature INVALID for ext_id={ExtId} signature={Signature}",
                LogScope, callback.ExtId, callback.Signature);
            return TypedResults.Unauthorized();
        }

        logger.LogInformation(
            "{Scope} ← webhook signature OK for ext_id={ExtId}",
            LogScope, callback.ExtId);

        // Acknowledge regardless of internal outcome to avoid endless bank retries.
        var result = await checkoutService.HandleCallbackAsync(callback, cancellationToken);

        logger.LogInformation(
            "{Scope} ← webhook handled for ext_id={ExtId} success={IsSuccess}",
            LogScope, callback.ExtId, result.IsSuccess);

        return TypedResults.Ok();
    }
}
