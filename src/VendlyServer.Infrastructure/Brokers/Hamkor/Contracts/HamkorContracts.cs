using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Brokers.Hamkor.Contracts;

// ── Token (POST /token, Basic auth) ───────────────────────────────────────────
public sealed record HamkorTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}

// ── JSON-RPC envelope ─────────────────────────────────────────────────────────
public sealed record HamkorRpcRequest<TParams>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    // The acquiring API always wraps params in a single-element array.
    [JsonPropertyName("params")]
    public required TParams[] Params { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

public sealed record HamkorRpcResponse<TResult>
{
    [JsonPropertyName("result")]
    public TResult? Result { get; init; }

    [JsonPropertyName("error")]
    public HamkorRpcError? Error { get; init; }
}

public sealed record HamkorRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

// ── pay.create.url ────────────────────────────────────────────────────────────

// One fiscal line for the invoice (cart item or delivery fee).
// AmountMinorUnits is the total for the line (price * qty) in tiyin.
public sealed record HamkorCreatePaymentItem(long AmountMinorUnits, int Qty);

// Input contract for the broker. Add new fields here when the payment request grows.
public sealed record HamkorCreatePaymentUrlRequest
{
    public required string ExternalId { get; init; }

    // amount in tiyin — smallest currency unit (so'm * 100)
    public required long AmountMinorUnits { get; init; }

    public required string SuccessUrl { get; init; }

    public required string FailureUrl { get; init; }

    public required string CallbackUrl { get; init; }

    // Fiscal lines — sum of (AmountMinorUnits * Qty) must equal AmountMinorUnits above.
    public required IReadOnlyList<HamkorCreatePaymentItem> Items { get; init; }
}

public sealed record HamkorCreateUrlParams
{
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; init; }

    // amount in tiyin — smallest currency unit (so'm * 100)
    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("success_url")]
    public required string SuccessUrl { get; init; }

    [JsonPropertyName("failure_url")]
    public required string FailureUrl { get; init; }

    [JsonPropertyName("callback_url")]
    public required string CallbackUrl { get; init; }

    // 1 = false (charge immediately), 2 = true (hold then capture).
    [JsonPropertyName("hold")]
    public int Hold { get; init; } = 1;

    // Fiscal data — required by the bank's hosted payment page to register a Soliq receipt.
    // Without this the page shows "wrong invoice" (error_code 1014).
    [JsonPropertyName("fiscal_data")]
    public HamkorFiscalData? FiscalData { get; init; }
}

public sealed record HamkorFiscalData
{
    [JsonPropertyName("item")]
    public required HamkorFiscalItem[] Item { get; init; }

    [JsonPropertyName("location")]
    public required HamkorLocation Location { get; init; }

    [JsonPropertyName("tin")]
    public required string Tin { get; init; }

    [JsonPropertyName("vat_percent")]
    public required int VatPercent { get; init; }
}

public sealed record HamkorFiscalItem
{
    // Line amount in tiyin (price * qty).
    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("package_code")]
    public required string PackageCode { get; init; }

    [JsonPropertyName("spic")]
    public required string Spic { get; init; }

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; } = [""];
}

public sealed record HamkorLocation
{
    [JsonPropertyName("lat")]
    public required double Lat { get; init; }

    [JsonPropertyName("long")]
    public required double Long { get; init; }
}

public sealed record HamkorCreateUrlResult
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

// ── pay.get.inv ───────────────────────────────────────────────────────────────
public sealed record HamkorExtIdParams
{
    [JsonPropertyName("ext_id")]
    public required string ExtId { get; init; }
}

public sealed record HamkorInvoiceResult
{
    [JsonPropertyName("state")]
    public int State { get; init; }

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("pay_id")]
    public string? PayId { get; init; }

    [JsonPropertyName("rrn")]
    public string? Rrn { get; init; }
}
