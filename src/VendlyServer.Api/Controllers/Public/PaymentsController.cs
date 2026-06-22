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
public sealed class PaymentsController(IEnumerable<IPaymentProvider> providers) : ControllerBase
{
    /// <summary>Payment provider callback (Payme JSON-RPC / Click form). Auth is protocol-level.</summary>
    [AllowAnonymous]
    [HttpPost("webhook/{provider}")]
    public async Task<IResult> WebhookAsync(string provider, CancellationToken cancellationToken = default)
    {
        var paymentProvider = providers.SingleOrDefault(
            p => p.Name.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (paymentProvider is null)
            return Results.NotFound();

        return await paymentProvider.HandleWebhookAsync(Request, cancellationToken);
    }
}
