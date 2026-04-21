using VendlyServer.Domain.Enums;
using VendlyServer.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendlyServer.Domain.Entities.Public;

[Table("users", Schema = "public")]
public class User : AuditableModelBase<long>
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsBlocked { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
}
