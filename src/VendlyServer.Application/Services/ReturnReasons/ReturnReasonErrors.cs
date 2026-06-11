using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.ReturnReasons;

public static class ReturnReasonErrors
{
    public static readonly Error NotFound  = Error.NotFound("ReturnReason.NotFound");
    public static readonly Error KeyExists = Error.Conflict("ReturnReason.KeyExists");
}
