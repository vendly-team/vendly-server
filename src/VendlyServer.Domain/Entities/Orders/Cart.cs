using System.Text.Json;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("carts", Schema = "orders")]
public class Cart : AuditableModelBase<long>
{
    public long? UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string SessionId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
