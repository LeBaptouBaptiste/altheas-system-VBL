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
///   <item><c>disable</c> and <c>recovery-codes/regenerate</c> require a normal
///         access token AND ask for password + a fresh 2FA code in the body.
///         These will move to a header-based step-up flow in commit 5.</item>
///   <item><c>status</c> just reads the user's flags.</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/auth/2fa")]
[EnableRateLimiting("auth")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactor;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public TwoFactorController(
        ITwoFactorService twoFactor,
        IAuthService authService,
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _twoFactor = twoFactor;
        _authService = authService;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    // ─────────────────────────────────────────────────────────
    //  setup + enable: open to access tokens AND setup tokens
    // ─────────────────────────────────────────────────────────

    [HttpPost("setup")]
    public async Task<ActionResult<TwoFactorSetupResult>> Setup()
    {
        var user = await ResolveUserFromAccessOrSetupTokenAsync();
        var result = await _twoFactor.StartSetupAsync(user);
        return Ok(result);
    }

    [HttpPost("enable")]
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
    //  disable + regenerate: inline password+code (pre-step-up)
    //
    //  Both endpoints will be replaced by the [RequireStepUp] attribute
    //  in commit 5: the body shrinks to nothing (or a step-up token in
    //  a header) and the password/code check moves to /auth/step-up.
    // ─────────────────────────────────────────────────────────

    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] DisableTwoFactorRequest request)
    {
        var user = await GetCurrentUserAsync();
        await RequireValidPasswordAndCodeAsync(user, request.Password, request.Code);

        await _twoFactor.DisableAsync(user);
        return NoContent();
    }

    [HttpPost("recovery-codes/regenerate")]
    [Authorize]
    public async Task<ActionResult<RegenerateRecoveryCodesResponse>> RegenerateRecoveryCodes(
        [FromBody] RegenerateRecoveryCodesRequest request)
    {
        var user = await GetCurrentUserAsync();
        await RequireValidPasswordAndCodeAsync(user, request.Password, request.Code);

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

    private async Task RequireValidPasswordAndCodeAsync(User user, string password, string code)
    {
        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid password.");

        var verify = await _twoFactor.VerifyAsync(user, code);
        if (verify.Outcome != TwoFactorVerifyOutcome.Valid
            && verify.Outcome != TwoFactorVerifyOutcome.ValidViaRecoveryCode)
        {
            throw new UnauthorizedException("Invalid 2FA code.");
        }
    }
}
