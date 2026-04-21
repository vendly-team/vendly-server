using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("discounts", Schema = "catalogs")]
public class Discount : AuditableModelBase<long>
{
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    public DiscountType Type { get; set; }

    public decimal Value { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DiscountProduct> DiscountProducts { get; set; } = [];
}
