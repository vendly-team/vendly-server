using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Catalogs;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("cart_items", Schema = "orders")]
public class CartItem : AuditableModelBase<long>
{
    public long CartId { get; set; }

    public long ProductVariantId { get; set; }

    public int Qty { get; set; }

    [ForeignKey(nameof(CartId))]
    public Cart Cart { get; set; } = null!;

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant ProductVariant { get; set; } = null!;
}
