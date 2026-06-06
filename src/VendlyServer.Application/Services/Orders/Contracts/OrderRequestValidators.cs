using FluentValidation;

namespace VendlyServer.Application.Services.Orders.Contracts;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class AddOrderNoteRequestValidator : AbstractValidator<AddOrderNoteRequest>
{
    public AddOrderNoteRequestValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(2000);
    }
}
