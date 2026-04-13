using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace VendlyServer.Domain.Entities.Catalog;

[Table("product_images", Schema = "catalog")]
public class ProductImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long ProductId { get; set; }

    [Required]
    public required string Url { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
