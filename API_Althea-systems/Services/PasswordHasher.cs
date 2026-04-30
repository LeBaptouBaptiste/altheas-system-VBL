using System.Text;
using System.Text.RegularExpressions;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public partial class PasswordHasherService : IPasswordHasher
{
    private const int WorkFactor = 12;
    // BCrypt silently truncates inputs longer than 72 bytes — reject explicitly to avoid surprises.
    private const int MaxBcryptByteLength = 72;

    public string Hash(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        if (Encoding.UTF8.GetByteCount(password) > MaxBcryptByteLength)
            throw new ArgumentException(
                $"Password exceeds the {MaxBcryptByteLength}-byte BCrypt limit.",
                nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash) || password is null) return false;
        // Reject oversized inputs to prevent prefix-only matches caused by BCrypt's silent truncation.
        if (Encoding.UTF8.GetByteCount(password) > MaxBcryptByteLength) return false;
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
