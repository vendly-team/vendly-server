using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Click.Contracts;

// Click Prepare/Complete callback'lariga kutadigan JSON javob tanasi.
public record ClickWebhookResponse
{
    [JsonPropertyName("click_trans_id")]
    public long ClickTransId { get; init; }

    [JsonPropertyName("merchant_trans_id")]
    public string MerchantTransId { get; init; } = string.Empty;

    [JsonPropertyName("merchant_prepare_id")]
    public long? MerchantPrepareId { get; init; }

    [JsonPropertyName("merchant_confirm_id")]
    public long? MerchantConfirmId { get; init; }

    [JsonPropertyName("error")]
    public int Error { get; init; }

    [JsonPropertyName("error_note")]
    public string ErrorNote { get; init; } = string.Empty;
}
