using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("variant_options", Schema = "catalogs")]
public class VariantOption : AuditableModelBase<long>
{
    public long VariantTypeId { get; set; }

    [ForeignKey(nameof(VariantTypeId))]
    public VariantType VariantType { get; set; } = null!;

    [MaxLength(100)]
    public required string Name { get; set; }  // "Sabzirang", "13 pro"
    
    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public int? DisplayOrder { get; set; }
}