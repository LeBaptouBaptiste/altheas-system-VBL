using FluentValidation;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Validators;

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .MaximumLength(200);
        });

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email!)
                .NotEmpty().WithMessage("Email cannot be empty.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(320);
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status!.Value)
                .IsInEnum().WithMessage("Invalid user status.");
        });
    }
}

public class AddressCreateRequestValidator : AbstractValidator<AddressCreateRequest>
{
    public AddressCreateRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(100);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Company)
            .MaximumLength(200);

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required.")
            .MaximumLength(200);

        RuleFor(x => x.Street2)
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20)
            .Matches(@"^[A-Za-z0-9\s\-]{2,20}$").WithMessage("Invalid postal code format.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .Matches(@"^[\+\d\s\-\(\)]{6,30}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid phone format.");
    }
}

public class PaymentMethodCreateRequestValidator : AbstractValidator<PaymentMethodCreateRequest>
{
    public PaymentMethodCreateRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .MaximumLength(50);

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(100);
    }
}
