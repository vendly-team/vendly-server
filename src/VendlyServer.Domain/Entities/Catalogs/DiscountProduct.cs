using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("discount_products", Schema = "catalogs")]
public class DiscountProduct : AuditableModelBase<long>
{
    public long DiscountId { get; set; }

    public long ProductId { get; set; }

    [ForeignKey(nameof(DiscountId))]
    public Discount Discount { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
