using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Payments.Click;

public static class ClickBrokerErrors
{
    public static readonly Error GetPaymentStatusFailed = Error.Failure("Click.GetPaymentStatus.Failed");
}
