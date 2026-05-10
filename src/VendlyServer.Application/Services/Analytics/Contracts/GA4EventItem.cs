namespace VendlyServer.Application.Services.Analytics.Contracts;

public sealed record GA4EventItem(
    string ItemId,
    string ItemName,
    decimal Price,
    int Quantity,
    string? ItemCategory = null
);
