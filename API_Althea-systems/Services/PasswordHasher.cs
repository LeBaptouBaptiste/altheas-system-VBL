using System.Text.RegularExpressions;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public partial class PasswordHasherService : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public bool MeetsRequirements(string password, out IList<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
            return false;
        }

        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters long.");

        if (password.Length > 128)
            errors.Add("Password must not exceed 128 characters.");

        if (!UppercaseRegex().IsMatch(password))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!LowercaseRegex().IsMatch(password))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!DigitRegex().IsMatch(password))
            errors.Add("Password must contain at least one digit.");

        if (!SpecialCharRegex().IsMatch(password))
            errors.Add("Password must contain at least one special character.");

        return errors.Count == 0;
    }

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")]
    private static partial Regex SpecialCharRegex();
}
