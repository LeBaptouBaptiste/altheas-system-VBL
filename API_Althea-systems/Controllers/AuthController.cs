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
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        // 201 with the user dto — NO tokens. The user must confirm their
        // email (link mailed to them) before they can log in.
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
    /// Consumes the confirmation token mailed at registration and flips
    /// EmailConfirmed=true on the user. Failure modes (token_expired,
    /// token_consumed, invalid_token) surface as 400 with a machine-readable
    /// `reason` the front uses to pick the right user copy.
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request);
        return Ok(new { message = "Email confirmed." });
    }

    /// <summary>
    /// Re-issues a confirmation link for an unconfirmed user. Always returns
    /// 200 — unknown / already-confirmed / throttled emails all look the
    /// same to the caller (anti-enumeration). Throttled at 1 mail / 5 min
    /// per user inside <see cref="AuthService.ResendConfirmationAsync"/>;
    /// the IP-level "auth" rate-limit policy still applies on top.
    /// </summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        await _authService.ResendConfirmationAsync(request);
        return Ok(new { message = "If this email matches an unconfirmed account, a new confirmation link has been sent." });
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
