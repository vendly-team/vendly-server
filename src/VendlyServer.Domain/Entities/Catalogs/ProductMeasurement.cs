using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("product_measurements", Schema = "catalogs")]
public class ProductMeasurement : AuditableModelBase<long>
{
    public long ProductVariantId { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? LengthCm { get; set; }

    public decimal? WidthCm { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? VolumeCm3 { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant ProductVariant { get; set; } = null!;
}
