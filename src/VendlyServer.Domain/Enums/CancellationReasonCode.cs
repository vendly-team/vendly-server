namespace VendlyServer.Domain.Enums;

public enum CancellationReasonCode
{
    CustomerRequest, OutOfStock, PaymentFailed,
    DuplicateOrder, DeliveryIssue, Other
}
