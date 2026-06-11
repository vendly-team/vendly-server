using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Application.Services.Faqs.Contracts;

public record CreateFaqRequest(
    MultiLanguageField Question,
    MultiLanguageField Answer);
