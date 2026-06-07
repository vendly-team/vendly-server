using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("categories", Schema = "catalogs")]
public class Category : AuditableModelBase<long>
{
    public MultiLanguageField Name { get; set; } = new();

    [MaxLength(300)]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonDocument? Metadata { get; set; }

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Discount> Discounts { get; set; } = [];
}
