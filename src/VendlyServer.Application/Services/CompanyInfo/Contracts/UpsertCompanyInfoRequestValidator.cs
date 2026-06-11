using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.CompanyInfo.Contracts;

public class UpsertCompanyInfoRequestValidator : AbstractValidator<UpsertCompanyInfoRequest>
{
    public UpsertCompanyInfoRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email address.");

        RuleFor(x => x.Logo)
            .Must(BeImage).When(x => x.Logo is not null)
            .WithMessage("Logo must be an image file.");

        RuleFor(x => x.OfertaUz).Must(BePdf).When(x => x.OfertaUz is not null).WithMessage("Oferta (uz) must be a PDF file.");
        RuleFor(x => x.OfertaRu).Must(BePdf).When(x => x.OfertaRu is not null).WithMessage("Oferta (ru) must be a PDF file.");
        RuleFor(x => x.OfertaEn).Must(BePdf).When(x => x.OfertaEn is not null).WithMessage("Oferta (en) must be a PDF file.");
        RuleFor(x => x.OfertaCyrl).Must(BePdf).When(x => x.OfertaCyrl is not null).WithMessage("Oferta (cyrl) must be a PDF file.");
    }

    private static bool BePdf(IFormFile? file)
        => file is not null &&
           (file.ContentType == "application/pdf" ||
            file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

    private static bool BeImage(IFormFile? file)
        => file is not null && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
