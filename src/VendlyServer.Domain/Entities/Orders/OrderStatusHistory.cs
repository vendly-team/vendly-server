using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_status_histories", Schema = "orders")]
public class OrderStatusHistory : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public string? Note { get; set; }

    public long? ChangedBy { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(ChangedBy))]
    public User? ChangedByUser { get; set; }
}
