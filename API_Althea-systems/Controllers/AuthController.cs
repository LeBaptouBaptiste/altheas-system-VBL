using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API_Althea_systems.Controllers.IControllers;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase, IAuthController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created("", result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Final step of the 2FA login flow. Consumes the challengeToken
    /// returned by /auth/login and a 6-digit TOTP code (or a recovery
    /// code in xxxx-xxxx-xxxx-xxxx format).
    /// </summary>
    [HttpPost("2fa/verify")]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactorChallenge(
        [FromBody] VerifyTwoFactorChallengeRequest request)
    {
        var result = await _authService.CompleteTwoFactorChallengeAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Issues a step-up token. The caller must already be authenticated
    /// (regular access token); identity is then re-verified using either
    /// a fresh 2FA code (preferred) or a password (only allowed for
    /// purpose=Action on accounts without 2FA).
    /// </summary>
    [HttpPost("step-up")]
    [Authorize]
    public async Task<ActionResult<StepUpResponse>> StepUp([FromBody] StepUpRequest request)
    {
        var userId = Guid.Parse(HttpContext.Items["UserId"]?.ToString()!);
        var result = await _authService.StepUpAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = Guid.Parse(HttpContext.Items["UserId"]?.ToString()!);
        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// CRITICAL SECURITY ISSUE: the original implementation accepted the
    /// user's email address as the "confirmation token", allowing an
    /// attacker to confirm any account by guessing the email. The endpoint
    /// is preserved (the frontend still calls it) but neutralized until a
    /// proper signed single-use token table is implemented.
    /// TODO: implement signed single-use token table (EmailConfirmationTokens)
    ///       with 30 min TTL.
    /// </summary>
    [HttpPost("confirm-email")]
    public Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        IActionResult result = StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "This endpoint is not implemented yet. Contact support."
        });
        return Task.FromResult(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    /// <summary>
    /// CRITICAL SECURITY ISSUE: the original implementation used the user's
    /// email as the "reset token", which means anyone who knew an email
    /// could reset that account's password. The endpoint is preserved (the
    /// frontend still calls it) but neutralized until a signed single-use
    /// token table is implemented.
    /// TODO: implement signed single-use token table (PasswordResetTokens)
    ///       with 30 min TTL.
    /// </summary>
    [HttpPost("reset-password")]
    public Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        IActionResult result = StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "This endpoint is not implemented yet. Contact support."
        });
        return Task.FromResult(result);
    }

}
