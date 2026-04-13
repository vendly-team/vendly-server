using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_returns", Schema = "orders")]
public class OrderReturn : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public long RequestedBy { get; set; }

    public ReturnReasonCode ReasonCode { get; set; }

    public string? ReasonText { get; set; }

    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

    public long? ReviewedBy { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(RequestedBy))]
    public User RequestedByUser { get; set; } = null!;

    [ForeignKey(nameof(ReviewedBy))]
    public User? ReviewedByUser { get; set; }

    public ICollection<OrderReturnItem> Items { get; set; } = new List<OrderReturnItem>();
}
