using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Controllers.IControllers;

public interface IAuthController
{
    Task<ActionResult<AuthResponse>> Register(RegisterRequest request);
    Task<ActionResult<LoginResponse>> Login(LoginRequest request);
    Task<ActionResult<AuthResponse>> VerifyTwoFactorChallenge(VerifyTwoFactorChallengeRequest request);
    Task<ActionResult<UserDto>> GetMe();
    Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request);
    Task<IActionResult> ForgotPassword(ForgotPasswordRequest request);
    Task<IActionResult> ResetPassword(ResetPasswordRequest request);
}
