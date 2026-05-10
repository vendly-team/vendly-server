using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Diagnostics;

[Table("sync_logs", Schema = "logs")]
public class SyncLog : AuditableModelBase<long>
{
    [Required]
    [MaxLength(50)]
    public required string Source { get; set; }

    public SyncStatus Status { get; set; }

    public int TotalProcessed { get; set; }

    public int CreatedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int ErrorCount { get; set; }
    

    [MaxLength(500)]
    public string? RequestUrl { get; set; }

    public JsonDocument? RequestBody { get; set; }

    public JsonDocument? ErrorDetail { get; set; }
    public JsonDocument? Response { get; set; }

    public long? TriggeredBy { get; set; }

    public int? DurationMs { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    [ForeignKey(nameof(TriggeredBy))]
    public User? TriggeredByUser { get; set; }
}
