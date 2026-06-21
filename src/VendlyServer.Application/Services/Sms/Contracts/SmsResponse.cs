using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Sms.Contracts;

public record SmsResponse(
    long Id,
    string Phone,
    string Message,
    SmsStatus Status,
    string? RequestId,
    DateTime CreatedAt);
