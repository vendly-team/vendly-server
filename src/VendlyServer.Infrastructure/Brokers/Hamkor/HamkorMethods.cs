namespace VendlyServer.Infrastructure.Brokers.Hamkor;

/// <summary>JSON-RPC method names exposed by the Hamkorbank acquiring API.</summary>
public static class HamkorMethods
{
    public const string CreatePaymentUrl = "pay.create.url";
    public const string GetInvoice = "pay.get.inv";
}
