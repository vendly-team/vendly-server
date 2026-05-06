using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.RecentlyViewed;

public static class RecentlyViewedErrors
{
    public static readonly Error NotFound = Error.NotFound("RecentlyViewed.NotFound");
    public static readonly Error ProductNotFound = Error.NotFound("RecentlyViewed.ProductNotFound");
}
