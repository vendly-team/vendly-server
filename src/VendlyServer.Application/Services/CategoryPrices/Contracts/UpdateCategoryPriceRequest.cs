using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.CategoryPrices.Contracts;

public record UpdateCategoryPriceRequest(
    long CategoryId,
    PriceMarkupType MarkupType,
    decimal Value,
    decimal? RoundingStep,
    DateTime? StartDate,
    DateTime? EndDate
);
