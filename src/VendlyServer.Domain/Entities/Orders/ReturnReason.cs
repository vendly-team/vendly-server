using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Orders;

[Table("return_reasons", Schema = "orders")]
public class ReturnReason : AuditableModelBase<long>
{
    [MaxLength(100)]
    public required string Key { get; set; }

    public MultiLanguageField Name { get; set; } = new();
    public MultiLanguageField Description { get; set; } = new();
    public bool CanResell { get; set; }
}
