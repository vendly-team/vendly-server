using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.ReturnReasons.Contracts;

public record CreateReturnReasonRequest(
    string Key,
    MultiLanguageField Name,
    MultiLanguageField Description,
    bool CanResell);
