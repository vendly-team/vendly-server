using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Diagnostics;

[Table("sms_messages", Schema = "logs")]
public class SmsMessage : AuditableModelBase<long>
{
    [MaxLength(20)]
    public required string Phone { get; set; }

    public required string Message { get; set; }

    // Eskiz "id" (request_id, UUID) — status callback bilan bog'lash kaliti.
    [MaxLength(64)]
    public string? RequestId { get; set; }

    public SmsStatus Status { get; set; }

    // Eskiz'dan kelgan xom status (DELIVRD, EXPIRED, ...).
    [MaxLength(50)]
    public string? RawStatus { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    // Ixtiyoriy — agar SMS foydalanuvchiga bog'liq bo'lsa (OTP'da null bo'lishi mumkin).
    public long? UserId { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
