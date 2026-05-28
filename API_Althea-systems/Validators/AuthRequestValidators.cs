using FluentValidation;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(320);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(512);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(512);
    }
}

public class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        // Anti-enumeration: the service always returns success. The validator
        // only protects against junk input (empty, oversized, malformed).
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(320);
    }
}
