using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Catalog;

namespace VendlyServer.Domain.Entities.Orders;

[Table("cart_items", Schema = "orders")]
public class CartItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

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
