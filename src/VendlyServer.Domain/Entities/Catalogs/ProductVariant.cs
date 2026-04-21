using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("product_variants", Schema = "catalogs")]
public class ProductVariant : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public string? Name { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> Images { get; set; } = [];
    
    public JsonDocument? Metadata { get; set; }

    // Bu SKUning qaysi optionlar kombinatsiyasidan iboratligini belgilaydi
    // Rang:Sabzirang + Model:13pro → 2 ta VariantOptionValue yozuvi
    public ICollection<VariantOptionValue> OptionValues { get; set; } = [];
    
    public ProductMeasurement? Measurements { get; set; }
}
