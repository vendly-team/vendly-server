using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

public record PaymeErrorMessageResponse
{
    [JsonPropertyName("ru")]
    public string Ru { get; init; } = string.Empty;

    [JsonPropertyName("uz")]
    public string Uz { get; init; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; init; } = string.Empty;
}
