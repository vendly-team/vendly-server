using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Infrastructure.Payments;

namespace VendlyServer.Api.Controllers.Public;

// Yagona to'lov webhook endpoint'i. Payme/Click kabinetlari shu yerga POST qiladi:
// /api/payments/webhook/{provider} (masalan .../webhook/payme, .../webhook/click).
// Har provider o'z wire formatini (JSON-RPC / form) parse qilib javob beradi — ProblemDetails emas.
// Hamkor alohida HamkorController'da qoladi (u outbound broker + imzolangan webhook).
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IEnumerable<IPaymentProvider> providers,
    ILogger<PaymentsController> logger) : ControllerBase
{
    /// <summary>Payment provider callback (Payme JSON-RPC / Click form). Auth is protocol-level.</summary>
    [AllowAnonymous]
    [HttpPost("webhook/{provider}")]
    public async Task<IResult> WebhookAsync(string provider, CancellationToken cancellationToken = default)
    {
        // Har qanday webhook so'rovini darhol logga olamiz — Click/Payme bizgacha yetib kelganini ko'rish uchun.
        // Body keyinroq provider'da log'lanadi (har provider o'z formati bilan).
        logger.LogInformation(
            "Payment webhook received: provider={Provider} content-type={ContentType} content-length={ContentLength} remote-ip={RemoteIp}",
            provider,
            Request.ContentType,
            Request.ContentLength,
            HttpContext.Connection.RemoteIpAddress);

        var paymentProvider = providers.SingleOrDefault(
            p => p.Name.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (paymentProvider is null)
        {
            logger.LogWarning("Payment webhook: unknown provider '{Provider}' — returning 404", provider);
            return Results.NotFound();
        }

        return await paymentProvider.HandleWebhookAsync(Request, cancellationToken);
    }
}
