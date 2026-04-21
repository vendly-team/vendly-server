using System.Text.Json;
using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Catalogs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Orders;

[Table("orders", Schema = "orders")]
public class Order : AuditableModelBase<long>
{
    public long UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string OrderNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public decimal Subtotal { get; set; }

    public decimal DeliveryCost { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DeliveryCity { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DeliveryDistrict { get; set; }

    [Required]
    [MaxLength(255)]
    public required string DeliveryStreet { get; set; }

    [Required]
    [MaxLength(50)]
    public required string DeliveryHouse { get; set; }

    [MaxLength(255)]
    public string? DeliveryExtra { get; set; }

    [Required]
    [MaxLength(10)]
    public required string DeliveryBtsCityCode { get; set; }

    [MaxLength(50)]
    public string? BtsOrderId { get; set; }

    [MaxLength(50)]
    public string? BtsBarcode { get; set; }

    public string? BtsTrackingUrl { get; set; }

    public string? BtsStickerUrl { get; set; }

    public int? BtsLastStatusCode { get; set; }

    [MaxLength(100)]
    public string? BtsLastStatusName { get; set; }

    public DateTime? BtsLastStatusAt { get; set; }

    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

    public DateTime? DeliveredAt { get; set; }

    public long? DiscountId { get; set; }

    public JsonDocument? Metadata { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(DiscountId))]
    public Discount? Discount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<OrderNote> Notes { get; set; } = new List<OrderNote>();
    public Payment? Payment { get; set; }
    public OrderCancellation? Cancellation { get; set; }
    public OrderReturn? Return { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
