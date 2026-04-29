namespace API_Althea_systems.Models.Users;

/// <summary>
/// Returned by <c>StartSetupAsync</c>. The secret is in base32 (ready to
/// be typed into an authenticator app); the URI can be rendered as a QR
/// code by the frontend (any qrcode library will do).
/// </summary>
public record TwoFactorSetupResult(string Secret, string OtpAuthUri);

/// <summary>
/// Plaintext recovery codes are returned ONCE — at activation or when
/// regenerating. They are stored only as BCrypt hashes server-side.
/// </summary>
public record TwoFactorEnableResult(IReadOnlyList<string> RecoveryCodes);

public record TwoFactorStatus(bool Enabled, DateTime? EnabledAt, int RecoveryCodesRemaining);

public enum TwoFactorVerifyOutcome
{
    /// <summary>The submitted TOTP code matched and was not a replay.</summary>
    Valid,

    /// <summary>The submitted code matched a previously unused recovery code (now consumed).</summary>
    ValidViaRecoveryCode,

    /// <summary>Code did not match (or was a replay of an already-used TOTP step).</summary>
    Invalid,

    /// <summary>2FA is not enabled on this account — caller should not have asked.</summary>
    NotEnabled,

    /// <summary>Account is temporarily locked because too many wrong codes piled up.</summary>
    Locked,
}

public record TwoFactorVerifyResult(
    TwoFactorVerifyOutcome Outcome,
    /// <summary>When <see cref="Outcome"/> is <see cref="TwoFactorVerifyOutcome.Locked"/>, the remaining lock duration in seconds.</summary>
    int? RetryAfterSeconds = null);

// ── HTTP request / response DTOs for the /api/auth/2fa endpoints ──

public record EnableTwoFactorRequest(string Code);

/// <summary>
/// Response shape of /api/auth/2fa/enable. Contains the one-shot recovery
/// codes AND a fresh AuthResponse — because once /enable succeeds the user
/// has proven MFA, so we (re)issue a JWT with amr = "pwd mfa". This also
/// gives the admin-setup flow a usable token (they didn't have one yet).
/// </summary>
public record TwoFactorEnableResponse(
    IReadOnlyList<string> RecoveryCodes,
    AuthResponse Auth
);

public record RegenerateRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
