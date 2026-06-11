using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("carts", Schema = "orders")]
public class Cart : AuditableModelBase<long>
{
    public long UserId { get; set; }

    // false = open shopping cart; true = attached to an order
    public bool IsCheckedOut { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
