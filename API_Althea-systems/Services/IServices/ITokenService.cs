using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface ITokenService
{
    /// <summary>
    /// Full-access JWT (purpose = "access"). When <paramref name="mfaVerified"/>
    /// is true, the token carries amr = ["pwd","mfa"] (RFC 8176), which step-up
    /// guards can consult to require recent MFA.
    /// </summary>
    string GenerateAccessToken(User user, bool mfaVerified = false);

    /// <summary>
    /// Short-lived (5 min) token issued after a successful password check
    /// for a 2FA-enabled account. The only endpoint that accepts it is
    /// /api/auth/2fa/verify.
    /// </summary>
    string GenerateChallengeToken(User user);

    /// <summary>
    /// Short-lived (15 min) token issued when an admin logs in without
    /// 2FA. Grants enough access to call /api/auth/2fa/setup + /enable
    /// to bootstrap MFA, but nothing else.
    /// </summary>
    string GenerateAdminSetupToken(User user);

    /// <summary>Cryptographically random refresh token (opaque, server-side managed).</summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a single-purpose token (challenge, setup, step-up) and
    /// returns the user id if signature/expiry are valid AND the token's
    /// purpose claim equals <paramref name="expectedPurpose"/>.
    /// Returns null in every failure case (caller maps to 401).
    /// </summary>
    Guid? ValidateSpecialToken(string token, string expectedPurpose);
}
