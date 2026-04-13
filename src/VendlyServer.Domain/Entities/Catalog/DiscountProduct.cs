using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalog;

[Table("discount_products", Schema = "catalog")]
public class DiscountProduct
{
    public long DiscountId { get; set; }

    public long ProductId { get; set; }

    [ForeignKey(nameof(DiscountId))]
    public Discount Discount { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
