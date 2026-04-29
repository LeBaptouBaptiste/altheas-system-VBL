using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// TOTP-based two-factor authentication. Workflow:
///   1. <see cref="StartSetupAsync"/>      -> user scans QR / types secret in their app
///   2. <see cref="EnableAsync"/>          -> user proves they can read codes; recovery codes returned
///   3. <see cref="VerifyAsync"/>          -> on subsequent logins / step-ups
///   4. <see cref="DisableAsync"/>         -> caller has already validated the user identity
///
/// Recovery codes are accepted by <see cref="VerifyAsync"/> as a fallback
/// when the user has lost their authenticator app. Each code is single-use.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a fresh TOTP secret and stores it in Redis under a per-user
    /// key with a short TTL. The secret is NOT yet persisted to the User row —
    /// that only happens once <see cref="EnableAsync"/> succeeds.
    /// Calling this twice for the same user simply overwrites the pending secret.
    /// </summary>
    Task<TwoFactorSetupResult> StartSetupAsync(User user);

    /// <summary>
    /// Confirms setup: validates the submitted code against the pending secret,
    /// then encrypts and persists the secret on the user, generates fresh
    /// recovery codes, and returns the plaintext codes (shown once).
    /// Throws if no pending setup exists or if the code is wrong.
    /// </summary>
    Task<TwoFactorEnableResult> EnableAsync(User user, string code);

    /// <summary>
    /// Verifies a 6-digit TOTP code OR a recovery code (xxxx-xxxx-xxxx-xxxx).
    /// Replay-protected: a successful TOTP step cannot be reused within its
    /// validity window. A recovery code is marked consumed on success.
    /// </summary>
    Task<TwoFactorVerifyResult> VerifyAsync(User user, string codeOrRecovery);

    /// <summary>
    /// Disables 2FA: clears the secret on the user and deletes all recovery
    /// codes. Caller is responsible for having proven identity (password +
    /// fresh code) before invoking this.
    /// </summary>
    Task DisableAsync(User user);

    /// <summary>
    /// Invalidates all existing recovery codes and issues 10 new ones.
    /// Returns the plaintext codes (shown once).
    /// </summary>
    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(User user);

    /// <summary>UI helper: enabled state, activation date, codes remaining.</summary>
    Task<TwoFactorStatus> GetStatusAsync(User user);
}
