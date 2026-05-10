using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public static class SmartupErrors
{
    public static readonly Error GetCategoriesFailed = Error.Failure("Smartup.GetCategoriesFailed");
    public static readonly Error GetProductsFailed   = Error.Failure("Smartup.GetProductsFailed");
}
