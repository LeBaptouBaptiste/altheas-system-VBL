namespace API_Althea_systems.Common.Auth;

/// <summary>
/// Values for the "purpose" JWT claim. Tokens without this claim, or with
/// purpose = <see cref="Access"/>, are full-access bearer tokens. All other
/// values mark a single-purpose token (challenge, setup, step-up) that the
/// JwtMiddleware refuses to map onto an authenticated user — only the
/// dedicated endpoint that issues / consumes that token will accept it.
/// </summary>
public static class TokenPurpose
{
    /// <summary>Default purpose for normal access tokens (also the implicit value when the claim is absent).</summary>
    public const string Access = "access";

    /// <summary>Issued when login succeeds for a 2FA-enabled account; only consumed by /2fa/verify.</summary>
    public const string TwoFactorChallenge = "2fa_challenge";

    /// <summary>Issued when an admin logs in without 2FA enabled; only consumed by /2fa/setup and /2fa/enable.</summary>
    public const string TwoFactorSetupRequired = "2fa_setup_required";

    /// <summary>Single-use token for sensitive actions (60s TTL).</summary>
    public const string StepUpAction = "step_up_action";

    /// <summary>Reusable token granting access to /api/admin/* (~30 min TTL).</summary>
    public const string StepUpAdmin = "step_up_admin";
}
