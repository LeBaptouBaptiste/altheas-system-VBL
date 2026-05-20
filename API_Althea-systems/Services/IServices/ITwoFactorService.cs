using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Two-factor authentication. Supports two methods:
/// <list type="bullet">
///   <item><b>Authenticator</b> — TOTP via an app (Google Authenticator, 1Password…).
///     Flow: <see cref="StartSetupAsync"/> → user scans QR → <see cref="EnableAsync"/>.</item>
///   <item><b>Email</b> — 6-digit code mailed at each login. Flow:
///     <see cref="StartEmailSetupAsync"/> sends a one-shot code →
///     <see cref="EnableEmailAsync"/> activates after the user types it back.</item>
/// </list>
///
/// <see cref="VerifyAsync"/> dispatches on <c>User.TwoFactorMethod</c>, so
/// callers (login, step-up) don't have to know which method the user picked.
/// Recovery codes work for BOTH methods — they're a fallback when the user
/// loses access to their primary channel.
/// </summary>
public interface ITwoFactorService
{
    // ── Authenticator (TOTP) setup ───────────────────────

    /// <summary>
    /// Generates a fresh TOTP secret and stores it in Redis under a per-user
    /// key with a short TTL. The secret is NOT yet persisted to the User row —
    /// that only happens once <see cref="EnableAsync"/> succeeds.
    /// Calling this twice for the same user simply overwrites the pending secret.
    /// </summary>
    Task<TwoFactorSetupResult> StartSetupAsync(User user);

    /// <summary>
    /// Confirms Authenticator setup: validates the submitted TOTP code against
    /// the pending secret, then encrypts and persists the secret on the user,
    /// generates fresh recovery codes, and sets <c>TwoFactorMethod=Authenticator</c>.
    /// </summary>
    Task<TwoFactorEnableResult> EnableAsync(User user, string code);

    // ── Email setup (phase 4b) ───────────────────────────

    /// <summary>
    /// Starts the email-based 2FA setup. Generates a 6-digit code, persists
    /// its SHA-256 hash in Redis (10 min TTL), then mails the plaintext code
    /// to the user. The user types it back into <see cref="EnableEmailAsync"/>
    /// to confirm.
    /// </summary>
    Task StartEmailSetupAsync(User user);

    /// <summary>
    /// Confirms Email setup: validates the submitted code against the hash
    /// in Redis, sets <c>TwoFactorMethod=Email</c> + <c>TwoFactorEnabled=true</c>,
    /// generates fresh recovery codes. No TOTP secret is persisted on the user
    /// (the per-login code lives in Redis, not on the User row).
    /// </summary>
    Task<TwoFactorEnableResult> EnableEmailAsync(User user, string code);

    /// <summary>
    /// During login: if the user's method is Email, generates a fresh 6-digit
    /// code (invalidating any pending one), stores its hash, and mails the
    /// plaintext. No-op if the method is anything else.
    /// </summary>
    Task RequestLoginEmailCodeAsync(User user);

    // ── Verify / disable / regen ─────────────────────────

    /// <summary>
    /// Verifies a 6-digit code OR a recovery code (xxxx-xxxx-xxxx-xxxx).
    /// Dispatches on <c>User.TwoFactorMethod</c>:
    /// <list type="bullet">
    ///   <item>Authenticator → TOTP path with replay protection</item>
    ///   <item>Email → checks the Redis hash; single-use (consumed on success)</item>
    ///   <item>Recovery code path works for both methods</item>
    /// </list>
    /// </summary>
    Task<TwoFactorVerifyResult> VerifyAsync(User user, string codeOrRecovery);

    /// <summary>
    /// Disables 2FA regardless of method: clears the secret, deletes recovery
    /// codes, wipes any pending email code, resets method to None. Caller is
    /// responsible for having proven identity before invoking.
    /// </summary>
    Task DisableAsync(User user);

    /// <summary>Invalidates all existing recovery codes and issues 10 new ones.</summary>
    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(User user);

    /// <summary>UI helper: enabled state, activation date, codes remaining, method.</summary>
    Task<TwoFactorStatus> GetStatusAsync(User user);
}
