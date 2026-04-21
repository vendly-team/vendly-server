using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("categories", Schema = "catalogs")]
public class Category : AuditableModelBase<long>
{
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Slug { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonDocument? Metadata { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
}
