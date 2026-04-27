using FluentValidation;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Validators;

public class VerifyTwoFactorChallengeRequestValidator : AbstractValidator<VerifyTwoFactorChallengeRequest>
{
    public VerifyTwoFactorChallengeRequestValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty().WithMessage("Challenge token is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Must(TwoFactorCodeShape.IsValid)
            .WithMessage("Code must be 6 digits or a recovery code (xxxx-xxxx-xxxx-xxxx).");
    }
}

public class EnableTwoFactorRequestValidator : AbstractValidator<EnableTwoFactorRequest>
{
    public EnableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Must(s => s != null && s.Length == 6 && s.All(char.IsDigit))
            .WithMessage("Enable expects the 6-digit TOTP code from the authenticator app (recovery codes are not accepted here).");
    }
}

public class DisableTwoFactorRequestValidator : AbstractValidator<DisableTwoFactorRequest>
{
    public DisableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Must(TwoFactorCodeShape.IsValid)
            .WithMessage("Code must be 6 digits or a recovery code (xxxx-xxxx-xxxx-xxxx).");
    }
}

public class RegenerateRecoveryCodesRequestValidator : AbstractValidator<RegenerateRecoveryCodesRequest>
{
    public RegenerateRecoveryCodesRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Must(TwoFactorCodeShape.IsValid)
            .WithMessage("Code must be 6 digits or a recovery code (xxxx-xxxx-xxxx-xxxx).");
    }
}

/// <summary>
/// Cheap shape gate for 2FA codes. The cryptographic check is done by
/// TwoFactorService — this just rejects obviously malformed inputs early.
/// </summary>
internal static class TwoFactorCodeShape
{
    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var trimmed = code.Trim();

        // 6 digits
        if (trimmed.Length == 6 && trimmed.All(char.IsDigit)) return true;

        // 4-4-4-4 hex
        if (trimmed.Length == 19
            && trimmed[4] == '-' && trimmed[9] == '-' && trimmed[14] == '-')
        {
            for (var i = 0; i < trimmed.Length; i++)
            {
                if (i is 4 or 9 or 14) continue;
                var c = trimmed[i];
                var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        return false;
    }
}
