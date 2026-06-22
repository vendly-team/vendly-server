using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.ReturnReasons.Contracts;

public record ReturnReasonResponse(
    long Id,
    string Key,
    MultiLanguageField Name,
    MultiLanguageField Description,
    bool CanResell,
    DateTime CreatedAt);
