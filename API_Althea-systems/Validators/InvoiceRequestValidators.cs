using FluentValidation;
using API_Althea_systems.Models.Invoices;

namespace API_Althea_systems.Validators;

public class InvoiceCreateRequestValidator : AbstractValidator<InvoiceCreateRequest>
{
    public InvoiceCreateRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order id is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid invoice type.");
    }
}
