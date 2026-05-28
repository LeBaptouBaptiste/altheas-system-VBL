using FluentValidation;
using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Validators;

public class ContactMessageCreateRequestValidator : AbstractValidator<ContactMessageCreateRequest>
{
    public ContactMessageCreateRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(320);

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000);
    }
}
