using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_cancellations", Schema = "orders")]
public class OrderCancellation : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public long RequestedBy { get; set; }

    public CancellationReasonCode ReasonCode { get; set; }

    public string? ReasonText { get; set; }

    [Required]
    [MaxLength(20)]
    public required string CancelledByRole { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(RequestedBy))]
    public User RequestedByUser { get; set; } = null!;
}
