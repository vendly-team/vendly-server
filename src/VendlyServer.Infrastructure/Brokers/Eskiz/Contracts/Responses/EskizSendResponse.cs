using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Eskiz.Contracts.Responses;

public class EskizSendResponse
{
    // request_id (UUID) — keyinchalik callback/status uchun kalit.
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
