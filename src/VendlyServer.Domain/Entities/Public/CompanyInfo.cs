using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Public;

[Table("company_info", Schema = "public")]
public class CompanyInfo : AuditableModelBase<long>
{
    // ── Asosiy kontakt ──
    [MaxLength(255)] public string? Name { get; set; }
    [MaxLength(50)] public string? Phone { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(255)] public string? WorkingHours { get; set; }

    // ── Yuridik rekvizitlar ──
    [MaxLength(50)] public string? Inn { get; set; }
    [MaxLength(50)] public string? Mfo { get; set; }
    [MaxLength(255)] public string? BankName { get; set; }
    [MaxLength(50)] public string? AccountNumber { get; set; }

    // ── Ijtimoiy tarmoqlar ──
    [MaxLength(500)] public string? Telegram { get; set; }
    [MaxLength(500)] public string? Instagram { get; set; }
    [MaxLength(500)] public string? Facebook { get; set; }
    [MaxLength(500)] public string? YouTube { get; set; }

    // ── Brending ──
    [MaxLength(1000)] public string? LogoUrl { get; set; }
    [MaxLength(255)] public string? BrandName { get; set; }

    // ── Oferta — har til uchun PDF URL (uz/ru/en/cyrl) ──
    public MultiLanguageField OfertaUrl { get; set; } = new();
}
