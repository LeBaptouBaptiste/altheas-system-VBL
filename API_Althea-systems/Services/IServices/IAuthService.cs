using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface IAuthService
{
    /// <summary>
    /// Creates a Customer user with <c>EmailConfirmed = false</c> and emails a
    /// single-use confirmation link. NO access / refresh tokens are issued —
    /// the user must confirm before they can log in.
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Verifies password and returns one of four outcomes
    /// (see <see cref="LoginOutcome"/>): authenticated, 2FA required,
    /// admin must set up 2FA, or email confirmation pending.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Final step of the 2FA login flow: validates the challenge token + the
    /// user-supplied code (TOTP or recovery), and returns the real access &
    /// refresh tokens (with amr=mfa).
    /// </summary>
    Task<AuthResponse> CompleteTwoFactorChallengeAsync(VerifyTwoFactorChallengeRequest request);

    /// <summary>
    /// Issues fresh access + refresh tokens for an already-authenticated user
    /// (e.g. just after /2fa/enable). Updates LastLogin and applies the
    /// <paramref name="mfaVerified"/> flag to the access token's amr claim.
    /// </summary>
    Task<AuthResponse> IssueTokensAsync(User user, bool mfaVerified);

    /// <summary>
    /// Issues a step-up token. Caller must be authenticated already
    /// (regular access token); this method then re-verifies identity via
    /// the supplied 2FA code (preferred) or password (only valid for
    /// <c>StepUpPurpose.Action</c> on accounts without 2FA).
    /// </summary>
    Task<StepUpResponse> StepUpAsync(Guid userId, StepUpRequest request);

    Task<UserDto> GetCurrentUserAsync(Guid userId);

    /// <summary>
    /// Consumes a confirmation token (sent by email at registration) and
    /// flips <c>User.EmailConfirmed = true</c>. Throws
    /// <see cref="Common.Exceptions.BadRequestException"/> when the token
    /// is unknown, expired, or already used — the front renders each case
    /// distinctly via the response's <c>reason</c>.
    /// </summary>
    Task ConfirmEmailAsync(ConfirmEmailRequest request);

    /// <summary>
    /// Re-issues a fresh confirmation email for an unconfirmed user.
    /// Returns successfully whether or not the email matches a user
    /// (anti-enumeration), but only actually mails when the address maps
    /// to an unconfirmed account and the user hasn't requested one in
    /// the last 5 minutes (anti-spam).
    /// </summary>
    Task ResendConfirmationAsync(ResendConfirmationRequest request);

    /// <summary>
    /// Anti-enumeration: always succeeds from the caller's POV. If the email
    /// matches a real account and the user hasn't requested one in the last
    /// 5 minutes, issues a single-use reset token (30 min TTL) and mails the
    /// link. SMTP failures are swallowed so we don't leak which addresses
    /// exist.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Consumes the reset token mailed at /forgot-password, rotates the
    /// user's password hash, marks the token consumed. Failure modes
    /// (token_expired, token_consumed, invalid_token, weak_password,
    /// passwords_mismatch) surface as 400 with a machine-readable `reason`
    /// for the front to branch on.
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
