using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Diagnostics;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Domain.Entities.Public;

[Table("otps", Schema = "public")]
public class Otp : ModelBase<long>
{
    [Required]
    [MaxLength(20)]
    public required string Phone { get; set; }

    [Required]
    [MaxLength(6)]
    public required string Code { get; set; }

    public DateTime ExpiresAt { get; set; }

    public OtpType Type { get; set; }

    public int ResendCount { get; set; }

    public int Attempts { get; set; }

    public long? SmsMessageId { get; set; }

    [ForeignKey(nameof(SmsMessageId))]
    public SmsMessage? SmsMessage { get; set; }
}
