using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.CompanyInfo.Contracts;

public record CompanyInfoResponse(
    string? Name,
    string? Phone,
    string? Email,
    string? Address,
    string? WorkingHours,
    string? Inn,
    string? Mfo,
    string? BankName,
    string? AccountNumber,
    string? Telegram,
    string? Instagram,
    string? Facebook,
    string? YouTube,
    string? BrandName,
    string? LogoUrl,
    // Accept-Language bo'yicha bitta til URL'i, yoki "ALL" bilan to'liq obyekt
    MultiLanguageField OfertaUrl,
    DateTime? UpdatedAt
);
