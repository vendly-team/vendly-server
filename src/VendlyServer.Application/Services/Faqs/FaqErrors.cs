using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Faqs;

public static class FaqErrors
{
    public static readonly Error NotFound = Error.NotFound("Faq.NotFound");
}
