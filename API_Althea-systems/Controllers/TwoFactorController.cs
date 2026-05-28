using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

/// <summary>
/// 2FA management endpoints.
/// <list type="bullet">
///   <item><c>setup</c> and <c>enable</c> accept either a normal access token
///         (user voluntarily turns on 2FA) OR an admin-setup token (issued by
///         /auth/login when an admin has no 2FA yet).</item>
///   <item><c>disable</c> and <c>recovery-codes/regenerate</c> are gated by
///         <see cref="RequireStepUpAttribute"/> — caller must hold a fresh
///         single-use action step-up token in the X-Step-Up-Token header.</item>
///   <item><c>status</c> just reads the user's flags.</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/auth/2fa")]
public class TwoFactorController : ControllerBase
{
    // NOTE: rate-limit "auth" (10/min/IP) is applied per-endpoint instead of
    // class-wide, so the harmless GET /status (called every time the user
    // visits account/security) doesn't eat the budget meant for sensitive
    // write operations.
    private readonly ITwoFactorService _twoFactor;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public TwoFactorController(
        ITwoFactorService twoFactor,
        IAuthService authService,
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _twoFactor = twoFactor;
        _authService = authService;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    // ─────────────────────────────────────────────────────────
    //  setup + enable: open to access tokens AND setup tokens
    // ─────────────────────────────────────────────────────────

    [HttpPost("setup")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorSetupResult>> Setup()
    {
        var user = await ResolveUserFromAccessOrSetupTokenAsync();
        var result = await _twoFactor.StartSetupAsync(user);
        return Ok(result);
    }

    [HttpPost("enable")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorEnableResponse>> Enable([FromBody] EnableTwoFactorRequest request)
    {
        var user = await ResolveUserFromAccessOrSetupTokenAsync();

        var enableResult = await _twoFactor.EnableAsync(user, request.Code);

        // Successful TOTP proof => issue tokens with amr=mfa.
        // For an admin going through forced setup, this is also the moment
        // they receive their first real access token.
        var auth = await _authService.IssueTokensAsync(user, mfaVerified: true);

        return Ok(new TwoFactorEnableResponse(enableResult.RecoveryCodes, auth));
    }

    // ─────────────────────────────────────────────────────────
    //  Email-based setup (phase 4b)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Starts email-based 2FA setup. Sends a one-time code to the user's
    /// email. Caller must hold an access token (regular user opting in) or
    /// an admin setup token (forced enrolment). Idempotent — calling twice
    /// just reissues a fresh code, invalidating the prior one.
    /// </summary>
    [HttpPost("setup/email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SetupEmail()
    {
        var user = await ResolveUserFromAccessOrSetupTokenAsync();
        await _twoFactor.StartEmailSetupAsync(user);
        return Ok(new { message = "A code has been sent to your email." });
    }

    /// <summary>
    /// Confirms email-based 2FA setup. Verifies the 6-digit code the user
    /// typed back, flips method=Email + enabled=true, returns recovery codes
    /// + a fresh AuthResponse (same shape as the Authenticator /enable).
    /// </summary>
    [HttpPost("enable/email")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorEnableResponse>> EnableEmail(
        [FromBody] EnableTwoFactorEmailRequest request)
    {
        var user = await ResolveUserFromAccessOrSetupTokenAsync();
        var enableResult = await _twoFactor.EnableEmailAsync(user, request.Code);
        var auth = await _authService.IssueTokensAsync(user, mfaVerified: true);
        return Ok(new TwoFactorEnableResponse(enableResult.RecoveryCodes, auth));
    }

    /// <summary>
    /// During the 2FA login challenge, lets an Email-method user request a
    /// fresh 6-digit code (typo'd or expired). Validates the challengeToken
    /// (so randoms can't farm us as an SMTP relay) and rate-limited by
    /// the "auth" policy on top.
    /// </summary>
    [HttpPost("resend-code")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendCode([FromBody] ResendTwoFactorCodeRequest request)
    {
        var userId = _tokenService.ValidateSpecialToken(request.ChallengeToken,
            TokenPurpose.TwoFactorChallenge);
        if (userId is null)
        {
            throw new UnauthorizedException("Invalid or expired challenge token.");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value)
            ?? throw new UnauthorizedException("Authentication required.");

        // Same swallow-on-SMTP-failure pattern as the login flow — front
        // shows the same "code sent" UX whether or not delivery succeeded.
        try
        {
            await _twoFactor.RequestLoginEmailCodeAsync(user);
        }
        catch (Exception)
        {
            // Logged inside the sender chain; nothing to surface here.
        }

        return Ok(new { message = "A new code has been sent." });
    }

    // ─────────────────────────────────────────────────────────
    //  status: requires a normal access token
    // ─────────────────────────────────────────────────────────

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<TwoFactorStatus>> Status()
    {
        var user = await GetCurrentUserAsync();
        var status = await _twoFactor.GetStatusAsync(user);
        return Ok(status);
    }

    // ─────────────────────────────────────────────────────────
    //  Sensitive ops gated by step-up
    // ─────────────────────────────────────────────────────────

    [HttpPost("disable")]
    [Authorize, RequireStepUp(StepUpPurpose.Action), EnableRateLimiting("auth")]
    public async Task<IActionResult> Disable()
    {
        var user = await GetCurrentUserAsync();
        await _twoFactor.DisableAsync(user);
        return NoContent();
    }

    [HttpPost("recovery-codes/regenerate")]
    [Authorize, RequireStepUp(StepUpPurpose.Action), EnableRateLimiting("auth")]
    public async Task<ActionResult<RegenerateRecoveryCodesResponse>> RegenerateRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        var newCodes = await _twoFactor.RegenerateRecoveryCodesAsync(user);
        return Ok(new RegenerateRecoveryCodesResponse(newCodes));
    }

    // ─────────────────────────────────────────────────────────
    //  helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the User behind the current request, accepting EITHER
    /// a normal access token (populated by JwtMiddleware) OR an admin
    /// setup token (purpose = 2fa_setup_required).
    /// </summary>
    private async Task<User> ResolveUserFromAccessOrSetupTokenAsync()
    {
        // Normal access token path: JwtMiddleware has already populated UserId.
        var userIdStr = HttpContext.Items["UserId"]?.ToString();
        if (Guid.TryParse(userIdStr, out var accessUserId))
        {
            return await _userRepository.GetByIdAsync(accessUserId)
                ?? throw new UnauthorizedException("Authentication required.");
        }

        // Setup token path: explicitly validate the bearer header as a setup token.
        var auth = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["Bearer ".Length..].Trim();
            var setupUserId = _tokenService.ValidateSpecialToken(token, TokenPurpose.TwoFactorSetupRequired);
            if (setupUserId.HasValue)
            {
                return await _userRepository.GetByIdAsync(setupUserId.Value)
                    ?? throw new UnauthorizedException("Authentication required.");
            }
        }

        throw new UnauthorizedException("Authentication required.");
    }

    private async Task<User> GetCurrentUserAsync()
    {
        var userIdStr = HttpContext.Items["UserId"]?.ToString();
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedException("Authentication required.");

        return await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedException("Authentication required.");
    }
}
