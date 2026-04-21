using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("products", Schema = "catalogs")]
public class Product : AuditableModelBase<long>
{
    public long CategoryId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Slug { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Sku { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public DateTime? SaleEndsAt { get; set; }

    public int Stock { get; set; }

    public SyncSource SyncSource { get; set; } = SyncSource.Manual;

    public bool HasSynced { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public ProductMeasurement? Measurements { get; set; }
    public ProductSyncMeta? SyncMeta { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSpec> Specs { get; set; } = new List<ProductSpec>();
    public ICollection<ProductFieldOverride> FieldOverrides { get; set; } = new List<ProductFieldOverride>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();
}
