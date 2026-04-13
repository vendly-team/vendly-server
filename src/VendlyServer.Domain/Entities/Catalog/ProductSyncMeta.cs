using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Catalog;

[Table("product_sync_meta", Schema = "catalog")]
public class ProductSyncMeta : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ExternalId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ExternalSource { get; set; }

    public decimal? ExtPrice { get; set; }

    public int? ExtStock { get; set; }

    public decimal? ExtWeightKg { get; set; }

    public decimal? ExtLengthCm { get; set; }

    public decimal? ExtWidthCm { get; set; }

    public decimal? ExtHeightCm { get; set; }

    public SyncStatus LastSyncStatus { get; set; }

    public string? LastSyncError { get; set; }

    public JsonDocument? RawPayload { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
