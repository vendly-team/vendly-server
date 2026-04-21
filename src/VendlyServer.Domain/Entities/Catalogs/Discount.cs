using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("discounts", Schema = "catalogs")]
public class Discount : AuditableModelBase<long>
{
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DiscountType Type { get; set; }

    public decimal Value { get; set; }

    public DiscountScope Scope { get; set; }

    public long? CategoryId { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();
}
