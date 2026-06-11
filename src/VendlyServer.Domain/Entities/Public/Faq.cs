using System.ComponentModel.DataAnnotations.Schema;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Domain.Entities.Public;

[Table("faqs", Schema = "public")]
public class Faq : AuditableModelBase<long>
{
    public MultiLanguageField Question { get; set; } = new();
    public MultiLanguageField Answer { get; set; } = new();
}
