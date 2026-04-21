using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Catalogs;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("cart_items", Schema = "orders")]
public class CartItem : AuditableModelBase<long>
{
    public long CartId { get; set; }

    public long ProductId { get; set; }

    public int Qty { get; set; }

    public decimal PriceSnapshot { get; set; }

    public JsonDocument? Metadata { get; set; }

    public DateTime AddedAt { get; set; }

    [ForeignKey(nameof(CartId))]
    public Cart Cart { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
