using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("variant_types", Schema = "catalogs")]
public class VariantType : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [MaxLength(100)]
    public required string Name { get; set; }   // "Rang", "Model", "O'lcham"

    public int? DisplayOrder { get; set; }

    public ICollection<VariantOption> Options { get; set; } = [];
}