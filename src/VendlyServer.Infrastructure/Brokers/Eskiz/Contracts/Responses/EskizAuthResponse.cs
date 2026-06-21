using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Eskiz.Contracts.Responses;

public class EskizAuthResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public EskizTokenData? Data { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

public class EskizTokenData
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
