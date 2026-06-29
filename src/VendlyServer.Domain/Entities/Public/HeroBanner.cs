using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Public;

[Table("hero_banners", Schema = "public")]
public class HeroBanner : AuditableModelBase<long>
{
    // Text fields — each language stored as JSONB via OwnsOne
    public MultiLanguageField Title { get; set; } = new();
    public MultiLanguageField Subtitle { get; set; } = new();
    public MultiLanguageField? BadgeText { get; set; }
    public MultiLanguageField CtaText { get; set; } = new();

    [MaxLength(500)] public string CtaLink { get; set; } = "/";
    [MaxLength(1000)] public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
