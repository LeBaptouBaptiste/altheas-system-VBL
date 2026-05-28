using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

/// <summary>Successful step-up token validation: the holder is <see cref="UserId"/>, identified by <see cref="Jti"/> for single-use tracking.</summary>
public record StepUpTokenValidation(Guid UserId, string Jti);

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
    /// Issues a step-up token for the user. TTL and reusability depend on
    /// <paramref name="purpose"/> (see <see cref="StepUpPurpose"/>).
    /// </summary>
    string GenerateStepUpToken(User user, StepUpPurpose purpose);

    /// <summary>
    /// Validates a single-purpose token (challenge, setup) and returns the
    /// user id if signature/expiry are valid AND the token's purpose claim
    /// equals <paramref name="expectedPurpose"/>. Returns null in every
    /// failure case (caller maps to 401).
    /// </summary>
    Guid? ValidateSpecialToken(string token, string expectedPurpose);

    /// <summary>
    /// Same as <see cref="ValidateSpecialToken"/> but for step-up tokens —
    /// also returns the jti so the caller can mark a single-use token consumed.
    /// </summary>
    StepUpTokenValidation? ValidateStepUpToken(string token, StepUpPurpose expectedPurpose);
}
