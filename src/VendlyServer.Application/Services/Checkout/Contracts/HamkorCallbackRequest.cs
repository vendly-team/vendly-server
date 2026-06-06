using System.Text.Json.Serialization;

namespace VendlyServer.Application.Services.Checkout.Contracts;

/// <summary>Payload the bank POSTs to our callback_url after a hosted-page payment.</summary>
public record HamkorCallbackRequest
{
    [JsonPropertyName("signature")]
    public string? Signature { get; init; }

    [JsonPropertyName("ext_id")]
    public string ExtId { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public int State { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("error")]
    public HamkorCallbackError? Error { get; init; }
}

public record HamkorCallbackError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
