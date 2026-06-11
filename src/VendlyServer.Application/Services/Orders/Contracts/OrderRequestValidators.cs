using FluentValidation;
using VendlyServer.Domain.Enums;

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

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.ReasonCode)
            .NotEmpty().WithMessage("Reason code is required.")
            .Must(code => Enum.TryParse<CancellationReasonCode>(code, ignoreCase: true, out _))
            .WithMessage("Unknown cancellation reason code.");

        RuleFor(x => x.ReasonText)
            .NotEmpty().WithMessage("Reason text is required.")
            .MaximumLength(2000);
    }
}
