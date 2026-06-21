using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.Eskiz;

public static class EskizErrors
{
    public static readonly Error LoginFailed = Error.Failure("Eskiz.Login.Failed");
    public static readonly Error RefreshFailed = Error.Failure("Eskiz.Refresh.Failed");
    public static readonly Error TokenEmpty = Error.Failure("Eskiz.Token.Empty");

    public static readonly Error SendFailed = Error.Failure("Eskiz.Send.Failed");
    public static readonly Error GetBalanceFailed = Error.Failure("Eskiz.GetBalance.Failed");

    // Eskiz so'rovni qabul qildi-yu, lekin rad etdi (masalan, test rejimida ruxsatsiz matn).
    // Eskiz qaytargan xabar Message'da olib boriladi.
    public static Error Rejected(string? message) =>
        new("Eskiz.Send.Rejected", ErrorType.Failure, message);
}
