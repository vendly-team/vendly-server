using FluentValidation;

namespace VendlyServer.Application.Services.Users.Contracts;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        When(x => x.Email is not null, () => RuleFor(x => x.Email).EmailAddress());
        RuleFor(x => x.Role).IsInEnum();
    }
}
