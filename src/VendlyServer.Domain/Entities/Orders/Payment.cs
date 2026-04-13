using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Orders;

[Table("payments", Schema = "orders")]
public class Payment : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public PaymentProvider Provider { get; set; }

    public string? TransactionId { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }

    public JsonDocument? ProviderResponse { get; set; }

    public DateTime? PaidAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;
}
