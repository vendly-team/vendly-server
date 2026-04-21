using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Ref;

[Table("bts_branches", Schema = "ref")]
public class BtsBranchRef : AuditableModelBase<long>
{
    [Required]
    [MaxLength(10)]
    public required string RegionCode { get; set; }

    [Required]
    [MaxLength(10)]
    public required string CityCode { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    public required string Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? LatLong { get; set; }

    public JsonDocument? WorkingHours { get; set; }

    public DateTime SyncedAt { get; set; }
}
