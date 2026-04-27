using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Services.IServices;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Verifies password and returns one of three outcomes
    /// (see <see cref="LoginOutcome"/>): authenticated, 2FA required, or
    /// admin must set up 2FA.
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

    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
