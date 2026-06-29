using Microsoft.AspNetCore.Http;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.HeroBanners.Contracts;

public record UpdateHeroBannerRequest(
    MultiLanguageField Title,
    MultiLanguageField Subtitle,
    MultiLanguageField? BadgeText,
    MultiLanguageField CtaText,
    string CtaLink,
    int SortOrder,
    bool IsActive,
    IFormFile? Image
);
