using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Faqs.Contracts;

public record FaqResponse(
    long Id,
    MultiLanguageField Question,
    MultiLanguageField Answer,
    DateTime CreatedAt);
