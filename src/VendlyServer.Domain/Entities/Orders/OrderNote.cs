using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_notes", Schema = "orders")]
public class OrderNote : AuditableModelBase<long>
{
    public long OrderId { get; set; }

    public long AdminId { get; set; }

    [Required]
    public required string Note { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(AdminId))]
    public User Admin { get; set; } = null!;
}
