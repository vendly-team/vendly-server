using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public static class HamkorErrors
{
    public static readonly Error TokenFailed = Error.Failure("Hamkor.Token.Failed");
    public static readonly Error TokenEmpty = Error.Failure("Hamkor.Token.Empty");

    public static readonly Error CreatePaymentUrlFailed = Error.Failure("Hamkor.CreatePaymentUrl.Failed");
    public static readonly Error GetInvoiceFailed = Error.Failure("Hamkor.GetInvoice.Failed");
}
