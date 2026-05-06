namespace VendlyServer.Application.Services.RecentlyViewed.Contracts;

public record RecentlyViewedResponse(
    long Id,
    long ProductId,
    DateTime ViewedAt
);
