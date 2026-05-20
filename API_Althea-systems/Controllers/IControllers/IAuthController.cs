using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Controllers.IControllers;

public interface IAuthController
{
    Task<ActionResult<RegisterResponse>> Register(RegisterRequest request);
    Task<ActionResult<LoginResponse>> Login(LoginRequest request);
    Task<ActionResult<AuthResponse>> VerifyTwoFactorChallenge(VerifyTwoFactorChallengeRequest request);
    Task<ActionResult<StepUpResponse>> StepUp(StepUpRequest request);
    Task<ActionResult<UserDto>> GetMe();
    Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request);
    Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request);
    Task<IActionResult> ForgotPassword(ForgotPasswordRequest request);
    Task<IActionResult> ResetPassword(ResetPasswordRequest request);
}
