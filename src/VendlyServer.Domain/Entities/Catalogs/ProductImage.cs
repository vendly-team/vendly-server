using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("product_images", Schema = "catalogs")]
public class ProductImage :  AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [Required]
    public required string Url { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
