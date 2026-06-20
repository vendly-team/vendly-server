using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public PaymeErrorMessageResponse Message { get; init; } = new();

    [JsonPropertyName("data")]
    public string? Data { get; init; }
}
