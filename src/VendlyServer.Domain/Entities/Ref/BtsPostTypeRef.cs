using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Ref;

[Table("bts_post_types", Schema = "ref")]
public class BtsPostTypeRef
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public int BtsId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DateTime SyncedAt { get; set; }
}
