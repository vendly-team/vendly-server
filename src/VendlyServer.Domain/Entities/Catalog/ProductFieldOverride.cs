using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;

namespace VendlyServer.Domain.Entities.Catalog;

[Table("product_field_overrides", Schema = "catalog")]
public class ProductFieldOverride : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FieldName { get; set; }

    [Required]
    public required string ManualValue { get; set; }

    public string? ExtValue { get; set; }

    public long OverriddenBy { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(OverriddenBy))]
    public User OverriddenByUser { get; set; } = null!;
}
