namespace API_Althea_systems.Models.Users;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginRequest(
    string Email,
    string Password
);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);

public record ConfirmEmailRequest(string Token);

public record Verify2FaRequest(string Code);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// One of three mutually exclusive outcomes of a /auth/login call.
/// Front-ends should pattern-match on <see cref="Outcome"/>.
/// </summary>
public enum LoginOutcome
{
    /// <summary>Credentials valid, no 2FA required — <c>Auth</c> is populated.</summary>
    Authenticated,

    /// <summary>Credentials valid but 2FA is on — caller must POST /auth/2fa/verify with <c>ChallengeToken</c>.</summary>
    TwoFactorRequired,

    /// <summary>Admin without 2FA — caller must complete /auth/2fa/setup + /enable using <c>SetupToken</c> before getting access.</summary>
    TwoFactorSetupRequired,
}

public record LoginResponse(
    LoginOutcome Outcome,
    AuthResponse? Auth = null,
    string? ChallengeToken = null,
    string? SetupToken = null
);

public record VerifyTwoFactorChallengeRequest(string ChallengeToken, string Code);
