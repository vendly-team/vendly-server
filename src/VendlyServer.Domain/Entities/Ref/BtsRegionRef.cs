using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Ref;

[Table("bts_regions", Schema = "ref")]
public class BtsRegionRef
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DateTime SyncedAt { get; set; }
}
