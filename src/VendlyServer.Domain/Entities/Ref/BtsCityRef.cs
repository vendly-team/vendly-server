using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Ref;

[Table("bts_cities", Schema = "ref")]
public class BtsCityRef
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(10)]
    public required string RegionCode { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DateTime SyncedAt { get; set; }
}
