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
            .Must(BeValidShape).WithMessage("Code must be 6 digits or a recovery code (xxxx-xxxx-xxxx-xxxx).");
    }

    /// <summary>
    /// Accepts either:
    ///   - a 6-digit TOTP code, or
    ///   - a 19-char recovery code formatted as 4-4-4-4 hex.
    /// The actual cryptographic check happens in TwoFactorService — this is
    /// just a shape gate to fail malformed inputs early.
    /// </summary>
    private static bool BeValidShape(string code)
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
