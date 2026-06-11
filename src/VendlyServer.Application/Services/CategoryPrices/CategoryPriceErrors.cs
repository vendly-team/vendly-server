using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.CategoryPrices;

public static class CategoryPriceErrors
{
    public static readonly Error NotFound = Error.NotFound("CategoryPrice.NotFound");
    public static readonly Error CategoryNotFound = Error.NotFound("CategoryPrice.CategoryNotFound");
    public static readonly Error InvalidDateRange =
        Error.Validation("CategoryPrice.InvalidDateRange", "EndDate must be on or after StartDate.");
}
