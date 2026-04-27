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

    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Legacy endpoint still wired to the old mock — to be removed once
    /// the new TwoFactorController lands in commit 4b.
    /// </summary>
    Task Verify2FaAsync(Guid userId, Verify2FaRequest request);
}
