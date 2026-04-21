using System.Text.Json;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Public;

namespace VendlyServer.Domain.Entities.Ref
{
    [Table("addresses", Schema = "ref")]
    public class Address : AuditableModelBase<long>
    {
        public long UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Label { get; set; }

        [Required]
        [MaxLength(100)]
        public required string City { get; set; }

        [Required]
        [MaxLength(100)]
        public required string District { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Street { get; set; }

        [Required]
        [MaxLength(50)]
        public required string House { get; set; }

        [MaxLength(255)]
        public string? Extra { get; set; }

        [Required]
        [MaxLength(10)]
        public required string BtsCityCode { get; set; }

        public bool IsDefault { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
