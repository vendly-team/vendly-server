using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Catalogs;

// VariantOptionValue.cs — SKU <-> Option bog'lovchi jadval
[Table("variant_options", Schema = "catalogs")]
public class VariantOptionValue : AuditableModelBase<long>
{
    public long ProductVariantId { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant ProductVariant { get; set; } = null!;

    public long VariantOptionId { get; set; }

    [ForeignKey(nameof(VariantOptionId))]
    public VariantOption VariantOption { get; set; } = null!;
}