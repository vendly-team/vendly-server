using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Payments.Click.Contracts;

namespace VendlyServer.Infrastructure.Payments.Click;

// Click Merchant API v2 broker — har so'rovga `Auth: ...` header qo'yib chiqadi.
// Hozir faqat GET /payment/status — kelajakda refund (DELETE /payment/reversal/...) qo'shilishi mumkin.
public class ClickBroker(
    IHttpClientFactory httpClientFactory,
    IOptions<ClickOptions> config,
    ILogger<ClickBroker> logger) : IClickBroker
{
    private const string BaseUrl = "https://api.click.uz/v2/merchant";
    private readonly ClickOptions _config = config.Value;

    public async Task<Result<ClickPaymentStatus>> GetPaymentStatusAsync(
        long clickTransId,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("Click");
        var url = $"{BaseUrl}/payment/status/{_config.ServiceId}/{clickTransId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Auth", ClickAuthHeader.Build(_config.MerchantUserId, _config.SecretKey));
        request.Headers.Accept.Add(new("application/json"));

        logger.LogInformation("Click → GET /payment/status click_trans_id={ClickTransId}", clickTransId);

        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation(
            "Click ← /payment/status {Status} click_trans_id={ClickTransId} Body: {Body}",
            (int)response.StatusCode, clickTransId, body);

        if (!response.IsSuccessStatusCode)
            return ClickBrokerErrors.GetPaymentStatusFailed;

        var payload = await response.Content.ReadFromJsonAsync<ClickPaymentStatusResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Click /payment/status returned empty body.");

        if (payload.ErrorCode < 0)
        {
            logger.LogWarning(
                "Click /payment/status returned error: code={Code} note={Note}",
                payload.ErrorCode, payload.ErrorNote);
            return ClickBrokerErrors.GetPaymentStatusFailed;
        }

        return (ClickPaymentStatus)payload.PaymentStatus;
    }
}
