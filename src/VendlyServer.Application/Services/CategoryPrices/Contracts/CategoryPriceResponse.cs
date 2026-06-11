using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.CategoryPrices.Contracts;

public record CategoryPriceResponse(
    long Id,
    long CategoryId,
    PriceMarkupType MarkupType,
    decimal Value,
    decimal? RoundingStep,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
