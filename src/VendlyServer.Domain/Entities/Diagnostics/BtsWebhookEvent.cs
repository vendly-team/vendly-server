using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Diagnostics;

[Table("bts_webhook_events", Schema = "logs")]
public class BtsWebhookEvent : AuditableModelBase<long>
{
    [Required]
    [MaxLength(50)]
    public required string BtsOrderId { get; set; }

    public int StatusCode { get; set; }

    [Required]
    [MaxLength(100)]
    public required string StatusName { get; set; }

    public JsonDocument RawPayload { get; set; } = null!;

    public bool IsProcessed { get; set; }

    public string? Error { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
