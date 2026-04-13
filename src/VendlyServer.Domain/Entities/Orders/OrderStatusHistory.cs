using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_status_history", Schema = "orders")]
public class OrderStatusHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public string? Note { get; set; }

    public long? ChangedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(ChangedBy))]
    public User? ChangedByUser { get; set; }
}
