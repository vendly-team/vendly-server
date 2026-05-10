using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Diagnostics;

[Table("notifications", Schema = "logs")]
public class NotificationLog : AuditableModelBase<long>
{
    public long UserId { get; set; }

    public NotificationType Type { get; set; }

    public NotificationChannel Channel { get; set; }

    [MaxLength(255)]
    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public bool IsSent { get; set; }

    public JsonDocument? ProviderResponse { get; set; }

    public DateTime? SentAt { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
