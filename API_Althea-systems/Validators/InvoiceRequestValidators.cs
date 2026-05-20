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

public class IssueCreditNoteRequestValidator : AbstractValidator<IssueCreditNoteRequest>
{
    public IssueCreditNoteRequestValidator()
    {
        // Cheap shape gate — the deeper "must not exceed remaining
        // creditable amount" check lives in InvoiceService and surfaces
        // with reason="exceeds_remaining".
        RuleFor(x => x.AmountHT)
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000m).WithMessage("Amount is unreasonably large.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
