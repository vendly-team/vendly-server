using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("category_prices", Schema = "catalogs")]
public class CategoryPrice : AuditableModelBase<long>
{
    public long CategoryId { get; set; }

    // Percent → Value foiz (masalan 15), Fixed → Value soum miqdori
    public PriceMarkupType MarkupType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    // null bo'lsa default app settings step ishlatiladi
    [Column(TypeName = "decimal(18,2)")]
    public decimal? RoundingStep { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;
}
