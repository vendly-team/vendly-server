using Microsoft.AspNetCore.Http;

namespace VendlyServer.Infrastructure.Payments.Click.Contracts;

// Click SHOP API callback (application/x-www-form-urlencoded). Qiymatlar xom string
// sifatida saqlanadi, chunki MD5 sign aynan kelgan ko'rinishda hisoblanadi.
public record ClickWebhookRequest
{
    public string ClickTransId { get; init; } = string.Empty;
    public string ServiceId { get; init; } = string.Empty;
    public string? ClickPaydocId { get; init; }

    // Bizning Order.Id (redirect'dagi transaction_param).
    public string MerchantTransId { get; init; } = string.Empty;
    public string? MerchantPrepareId { get; init; }

    // Summa so'mda kasr bilan, masalan "59000.00".
    public string Amount { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string? ErrorNote { get; init; }
    public string SignTime { get; init; } = string.Empty;
    public string SignString { get; init; } = string.Empty;

    public static ClickWebhookRequest FromForm(IFormCollection form) => new()
    {
        ClickTransId = Value(form, "click_trans_id"),
        ServiceId = Value(form, "service_id"),
        ClickPaydocId = form.TryGetValue("click_paydoc_id", out var paydoc) ? paydoc.ToString() : null,
        MerchantTransId = Value(form, "merchant_trans_id"),
        MerchantPrepareId = form.TryGetValue("merchant_prepare_id", out var prepare) ? prepare.ToString() : null,
        Amount = Value(form, "amount"),
        Action = Value(form, "action"),
        Error = Value(form, "error"),
        ErrorNote = form.TryGetValue("error_note", out var note) ? note.ToString() : null,
        SignTime = Value(form, "sign_time"),
        SignString = Value(form, "sign_string"),
    };

    private static string Value(IFormCollection form, string key) =>
        form.TryGetValue(key, out var value) ? value.ToString() : string.Empty;
}
