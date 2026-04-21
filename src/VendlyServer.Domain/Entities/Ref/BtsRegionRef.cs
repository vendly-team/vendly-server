using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Ref;

[Table("bts_regions", Schema = "ref")]
public class BtsRegionRef : AuditableModelBase<long>
{
    [Required]
    [MaxLength(10)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DateTime SyncedAt { get; set; }
}
