using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace VendlyServer.Application.Services.HeroBanners.Contracts;

public class HeroBannerRequestValidator : AbstractValidator<CreateHeroBannerRequest>
{
    public HeroBannerRequestValidator()
    {
        RuleFor(x => x.Title.Uz)
            .NotEmpty().WithMessage("Title (UZ) is required.");

        RuleFor(x => x.CtaLink)
            .NotEmpty().WithMessage("CTA link is required.")
            .MaximumLength(500);

        RuleFor(x => x.Image)
            .Must(BeImage).When(x => x.Image is not null)
            .WithMessage("Image must be an image file.");
    }

    private static bool BeImage(IFormFile? file)
        => file is not null && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
