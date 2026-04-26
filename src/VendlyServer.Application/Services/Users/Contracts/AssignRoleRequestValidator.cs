using FluentValidation;

namespace VendlyServer.Application.Services.Users.Contracts;

public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
