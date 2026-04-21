using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_return_items", Schema = "orders")]
public class OrderReturnItem : AuditableModelBase<long>
{
    public long ReturnId { get; set; }

    public long OrderItemId { get; set; }

    public int Qty { get; set; }

    public string? Reason { get; set; }

    [ForeignKey(nameof(ReturnId))]
    public OrderReturn Return { get; set; } = null!;

    [ForeignKey(nameof(OrderItemId))]
    public OrderItem OrderItem { get; set; } = null!;
}
