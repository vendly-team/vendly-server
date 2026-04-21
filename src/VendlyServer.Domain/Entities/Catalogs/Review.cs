using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Entities.Public;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Catalogs;

[Table("reviews", Schema = "catalogs")]
public class Review : AuditableModelBase<long>
{
    public long ProductId { get; set; }

    public long UserId { get; set; }

    public long OrderId { get; set; }

    public short Rating { get; set; }

    [Required]
    public required string Body { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;
}
