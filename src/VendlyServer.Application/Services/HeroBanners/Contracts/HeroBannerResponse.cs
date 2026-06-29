using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.HeroBanners.Contracts;

public record HeroBannerResponse(
    long Id,
    MultiLanguageField Title,
    MultiLanguageField Subtitle,
    MultiLanguageField? BadgeText,
    MultiLanguageField CtaText,
    string CtaLink,
    string? ImageUrl,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
