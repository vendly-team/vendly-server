using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace VendlyServer.Domain.Entities.Catalog;

[Table("product_specs", Schema = "catalog")]
public class ProductSpec
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

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
