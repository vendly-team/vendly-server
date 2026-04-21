using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("product_specs", Schema = "catalogs")]
public class ProductSpec : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Key { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Value { get; set; }

    public int SortOrder { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
