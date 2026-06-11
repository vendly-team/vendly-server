using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.CompanyInfo.Contracts;

public record UpsertCompanyInfoRequest(
    // Asosiy kontakt
    string? Name,
    string? Phone,
    string? Email,
    string? Address,
    string? WorkingHours,
    // Yuridik rekvizitlar
    string? Inn,
    string? Mfo,
    string? BankName,
    string? AccountNumber,
    // Ijtimoiy tarmoqlar
    string? Telegram,
    string? Instagram,
    string? Facebook,
    string? YouTube,
    // Brending
    string? BrandName,
    IFormFile? Logo,
    // Oferta PDF — har til uchun
    IFormFile? OfertaUz,
    IFormFile? OfertaRu,
    IFormFile? OfertaEn,
    IFormFile? OfertaCyrl
);
