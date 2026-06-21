using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Sms;

public static class SmsErrors
{
    public static readonly Error InvalidPhone =
        Error.Validation("Sms.InvalidPhone", "Phone number must be in 998XXXXXXXXX format.");

    public static readonly Error EmptyMessage =
        Error.Validation("Sms.EmptyMessage", "Message must not be empty.");

    public static readonly Error SendFailed = Error.Failure("Sms.SendFailed");
    public static readonly Error BalanceFailed = Error.Failure("Sms.BalanceFailed");
    public static readonly Error NotFound = Error.NotFound("Sms.NotFound");
}
