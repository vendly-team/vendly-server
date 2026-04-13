using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Catalog;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_items", Schema = "orders")]
public class OrderItem : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public long? ProductId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string ProductNameSnap { get; set; }

    [Required]
    [MaxLength(100)]
    public required string SkuSnap { get; set; }

    [Required]
    public required string ImageSnap { get; set; }

    public decimal WeightKgSnap { get; set; }

    public int Qty { get; set; }

    public decimal PriceSnap { get; set; }

    public decimal TotalSnap { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public ICollection<OrderReturnItem> ReturnItems { get; set; } = new List<OrderReturnItem>();
}
