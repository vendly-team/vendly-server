using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("recently_viewed_products", Schema = "catalogs")]
public class RecentlyViewedProduct : AuditableModelBase<long>
{
    public long UserId { get; set; }

    public long ProductId { get; set; }

    [Column("viewed_at")]
    public DateTime ViewedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
