using FluentValidation;

namespace VendlyServer.Application.Services.Users.Contracts;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        When(x => x.Email is not null, () => RuleFor(x => x.Email).EmailAddress());
    }
}
