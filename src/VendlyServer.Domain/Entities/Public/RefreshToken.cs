using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Public;

[Table("refresh_tokens", Schema = "public")]
public class RefreshToken : AuditableModelBase<long>
{
    public long UserId { get; set; }

    [Required]
    public required string Token { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
