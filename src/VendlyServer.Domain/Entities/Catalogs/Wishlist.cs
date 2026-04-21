using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("wishlists", Schema = "catalogs")]
public class Wishlist : AuditableModelBase<long>
{
    public long UserId { get; set; }

    public long ProductId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
