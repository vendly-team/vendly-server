using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("payment_transactions", Schema = "orders")]
public class PaymentTransaction : AuditableModelBase<long>
{
    public long PaymentId { get; set; }

    public PaymentProvider Provider { get; set; }

    // Provider identifikatori (Payme: params.id, Click: click_trans_id) — provider bo'yicha unique.
    [MaxLength(64)]
    public required string ProviderTransactionId { get; set; }

    public PaymentTransactionState State { get; set; } = PaymentTransactionState.Created;

    // Summa tiyinda.
    public long Amount { get; set; }

    // Payme'ning o'z yaratilish vaqti (unix ms) — javoblarda qaytariladi.
    public long? PaymeTime { get; set; }

    public DateTimeOffset CreateTime { get; set; }

    public DateTimeOffset? PerformTime { get; set; }

    public DateTimeOffset? CancelTime { get; set; }

    public PaymentTransactionCancelReason? CancelReason { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;
}
