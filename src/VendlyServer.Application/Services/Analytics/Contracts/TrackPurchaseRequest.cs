namespace VendlyServer.Application.Services.Analytics.Contracts;

public sealed record TrackPurchaseRequest(
    string ClientId,
    string TransactionId,
    decimal Value,
    decimal Shipping,
    string Currency,
    List<GA4EventItem> Items
);
