using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Application.Services.Sms;
using VendlyServer.Application.Services.Sms.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/sms")]
public sealed class SmsCallbackController(
    ISmsService smsService,
    ILogger<SmsCallbackController> logger) : ControllerBase
{
    /// <summary>Delivery status callback from Eskiz.uz.</summary>
    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IResult> CallbackAsync(CancellationToken cancellationToken = default)
    {
        // Eskiz callback'i POST form sifatida keladi (snake_case maydonlar).
        if (!Request.HasFormContentType)
        {
            logger.LogWarning("Eskiz callback: unexpected content type {ContentType}", Request.ContentType);
            return TypedResults.Ok();
        }

        var form = await Request.ReadFormAsync(cancellationToken);

        var callback = new SmsStatusCallbackRequest
        {
            RequestId = form["request_id"],
            MessageId = form["message_id"],
            UserSmsId = form["user_sms_id"],
            Country = form["country"],
            PhoneNumber = form["phone_number"],
            Status = form["status"],
            StatusDate = ParseDate(form["status_date"]),
        };

        // Idempotent — natijadan qat'i nazar 200 qaytaramiz (Eskiz qayta urinmasin).
        await smsService.HandleCallbackAsync(callback, cancellationToken);
        return TypedResults.Ok();
    }

    // Eskiz vaqtni "2021-04-02 00:39:36" formatida yuboradi.
    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, styles, out var exact))
            return exact;

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, styles, out var parsed) ? parsed : null;
    }
}
