namespace API_Althea_systems.Models.Users;

/// <summary>
/// Single-use token issued by POST /auth/forgot-password and consumed by
/// POST /auth/reset-password. Same shape as <see cref="EmailConfirmationToken"/> —
/// SHA-256 hash stored, not the raw token — but a much shorter TTL (30 min)
/// because a leaked reset link gives full password takeover.
///
/// Lifecycle:
///   1. ForgotPasswordAsync generates a random token, persists the hash with
///      a 30 min expiry, mails the raw token as a URL.
///   2. The user clicks the link → POST /auth/reset-password with the raw
///      token and a new password.
///   3. ResetPasswordAsync hashes the token, looks up the row, validates
///      expiry/consumption, rotates the user's password, marks the token
///      consumed.
///   4. Subsequent uses of the same link → 400 "already used".
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Hex-encoded SHA-256 of the raw token. 64 chars exactly.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>UTC moment the token stops being valid (creation + 30 min).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Set when the token is successfully consumed. Never deleted —
    /// audit trail of "password reset for this user at T".
    /// </summary>
    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
