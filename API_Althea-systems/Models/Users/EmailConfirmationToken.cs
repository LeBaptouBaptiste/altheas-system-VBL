namespace API_Althea_systems.Models.Users;

/// <summary>
/// Single-use confirmation token issued at registration. We store only the
/// SHA-256 hash of the raw token — a DB dump can't be used to confirm
/// arbitrary accounts, the attacker would still need the bytes from the
/// original email link.
///
/// Lifecycle:
///   1. AuthService.RegisterAsync generates a fresh token + persists the hash
///      with a 24 h expiry, then emails the raw token as a URL.
///   2. The user clicks the link → POST /auth/confirm-email with the raw token.
///   3. AuthService.ConfirmEmailAsync hashes it, looks up the row, validates
///      expiry/consumption, then marks ConsumedAt and flips
///      User.EmailConfirmed = true in the same transaction.
///   4. Subsequent uses of the same link → 400 "already used".
/// </summary>
public class EmailConfirmationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Hex-encoded SHA-256 of the raw token. 64 chars exactly.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>UTC moment the token stops being valid (creation + 24 h).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Set when the token is successfully consumed by /auth/confirm-email.
    /// Null until then. We never delete consumed rows — they're an audit
    /// trail for "this email was confirmed at T".
    /// </summary>
    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
