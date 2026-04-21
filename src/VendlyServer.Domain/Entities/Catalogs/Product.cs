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

    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public SyncSource SyncSource { get; set; } = SyncSource.Manual;

    public bool IsActive { get; set; } = true;

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;
    
    public ICollection<VariantType> VariantTypes { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<Wishlist> Wishlists { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<DiscountProduct> DiscountProducts { get; set; } = [];
}
