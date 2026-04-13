using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Public;

namespace VendlyServer.Domain.Entities.Orders;

[Table("order_notes", Schema = "orders")]
public class OrderNote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long AdminId { get; set; }

    [Required]
    public required string Note { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(AdminId))]
    public User Admin { get; set; } = null!;
}
