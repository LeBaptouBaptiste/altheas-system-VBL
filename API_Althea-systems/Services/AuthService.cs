using System.Security.Cryptography;
using System.Text;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;

namespace API_Althea_systems.Services;

public class AuthService : IAuthService
{
    // Per-account brute-force protection: 5 failures within a 15 min window
    // trigger a 15 min lockout. The IP-based "auth" rate-limiter remains in
    // place as defence in depth; this complements it for distributed attacks.
    private const int MaxFailuresBeforeLock = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    // Resend cooldown: keeps the same user from spamming the confirmation
    // mailbox (and our SMTP quota). 5 minutes is short enough to feel
    // responsive after a typo'd email, long enough to deter abuse.
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(5);

    // Phase 5: same throttling logic for password reset emails. Same
    // 5 min so an attacker can't farm us as an SMTP relay or DoS a user's
    // inbox.
    private static readonly TimeSpan PasswordResetCooldown = TimeSpan.FromMinutes(5);

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _confirmationTokens;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly ITokenService _tokenService;
    private readonly ITwoFactorService _twoFactor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginAttemptStore _loginAttempts;
    private readonly IEmailConfirmationSender _confirmationSender;
    private readonly IPasswordResetSender _passwordResetSender;
    private readonly EmailConfirmationOptions _confirmationOptions;
    private readonly PasswordResetOptions _passwordResetOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository confirmationTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        ITokenService tokenService,
        ITwoFactorService twoFactor,
        IPasswordHasher passwordHasher,
        ILoginAttemptStore loginAttempts,
        IEmailConfirmationSender confirmationSender,
        IPasswordResetSender passwordResetSender,
        IOptions<EmailConfirmationOptions> confirmationOptions,
        IOptions<PasswordResetOptions> passwordResetOptions,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _confirmationTokens = confirmationTokens;
        _passwordResetTokens = passwordResetTokens;
        _tokenService = tokenService;
        _twoFactor = twoFactor;
        _passwordHasher = passwordHasher;
        _loginAttempts = loginAttempts;
        _confirmationSender = confirmationSender;
        _passwordResetSender = passwordResetSender;
        _confirmationOptions = confirmationOptions.Value;
        _passwordResetOptions = passwordResetOptions.Value;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new ConflictException("User", "email", request.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            EmailConfirmed = false
        };

        await _userRepository.CreateAsync(user);

        // Issue + send confirmation. SMTP failures don't roll back the
        // registration — the user can use POST /auth/resend-confirmation
        // to retry without re-registering.
        var (rawToken, _) = await IssueConfirmationTokenAsync(user);
        try
        {
            await _confirmationSender.SendAsync(user, rawToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Confirmation email failed to send for user {UserId} ({Email}). User can resend.",
                user.Id, user.Email);
        }

        return new RegisterResponse(MapToDto(user));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();

        // Account-level lockout — fires before we hit the database so a
        // locked-out attacker can't even probe whether an email exists.
        var lockRemaining = await _loginAttempts.GetLockRemainingAsync(normalizedEmail);
        if (lockRemaining is { } remaining && remaining > TimeSpan.Zero)
        {
            throw new AccountLockedException((int)Math.Ceiling(remaining.TotalSeconds));
        }

        var user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await RecordFailureAndMaybeLockAsync(normalizedEmail);
            throw new UnauthorizedException("Invalid email or password.", reason: "invalid_credentials");
        }

        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("This account has been deactivated.");

        // Successful password check — clear any partial failure trail before
        // dispatching to the 2FA / setup / regular branch.
        await _loginAttempts.ResetAsync(normalizedEmail);

        // Branch 0 (new in phase 2): email not yet confirmed → block any
        // token issuance, tell the front to show the "check inbox" screen.
        // This MUST come before 2FA branches because an unconfirmed user
        // can't have set up 2FA anyway (the user-flow forces confirmation
        // first).
        if (!user.EmailConfirmed)
        {
            return new LoginResponse(LoginOutcome.EmailConfirmationRequired);
        }

        // Branch 1: 2FA enabled -> issue a challenge, do NOT update LastLogin yet
        // (that happens once the code is verified).
        if (user.TwoFactorEnabled)
        {
            // Phase 4b: for email-based 2FA, fire the per-login code mail
            // BEFORE returning the challenge so the user finds the code in
            // their inbox by the time the front asks for it. Swallow SMTP
            // failures — the challenge still goes out and the user can
            // request a resend.
            if (user.TwoFactorMethod == Common.Enums.TwoFactorMethod.Email)
            {
                try
                {
                    await _twoFactor.RequestLoginEmailCodeAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send login 2FA email code for user {UserId} ({Email}). " +
                        "Challenge token still issued; user can resend.",
                        user.Id, user.Email);
                }
            }

            var challenge = _tokenService.GenerateChallengeToken(user);
            return new LoginResponse(
                LoginOutcome.TwoFactorRequired,
                ChallengeToken: challenge,
                TwoFactorMethod: user.TwoFactorMethod);
        }

        // Branch 2: admin without 2FA -> force enrollment before access.
        if (user.Role == UserRole.Admin)
        {
            var setupToken = _tokenService.GenerateAdminSetupToken(user);
            return new LoginResponse(LoginOutcome.TwoFactorSetupRequired, SetupToken: setupToken);
        }

        // Branch 3: regular login.
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user, mfaVerified: false);
        var refreshToken = _tokenService.GenerateRefreshToken();
        return new LoginResponse(
            LoginOutcome.Authenticated,
            Auth: new AuthResponse(accessToken, refreshToken, MapToDto(user)));
    }

    private async Task RecordFailureAndMaybeLockAsync(string normalizedEmail)
    {
        var failures = await _loginAttempts.IncrementFailureAsync(normalizedEmail, FailureWindow);
        if (failures >= MaxFailuresBeforeLock)
        {
            await _loginAttempts.LockAsync(normalizedEmail, LockDuration);
        }
    }

    public async Task<AuthResponse> CompleteTwoFactorChallengeAsync(VerifyTwoFactorChallengeRequest request)
    {
        var userId = _tokenService.ValidateSpecialToken(request.ChallengeToken, TokenPurpose.TwoFactorChallenge)
            ?? throw new UnauthorizedException("Invalid or expired challenge token.");

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedException("Invalid or expired challenge token.");

        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("This account has been deactivated.");

        var verify = await _twoFactor.VerifyAsync(user, request.Code);
        if (verify.Outcome == TwoFactorVerifyOutcome.Locked)
        {
            throw new AccountLockedException(verify.RetryAfterSeconds ?? 900);
        }
        if (verify.Outcome != TwoFactorVerifyOutcome.Valid
            && verify.Outcome != TwoFactorVerifyOutcome.ValidViaRecoveryCode)
        {
            throw new UnauthorizedException("Invalid 2FA code.", reason: "invalid_credentials");
        }

        return await IssueTokensAsync(user, mfaVerified: true);
    }

    public async Task<AuthResponse> IssueTokensAsync(User user, bool mfaVerified)
    {
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user, mfaVerified);
        var refreshToken = _tokenService.GenerateRefreshToken();
        return new AuthResponse(accessToken, refreshToken, MapToDto(user));
    }

    public async Task<StepUpResponse> StepUpAsync(Guid userId, StepUpRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedException("Authentication required.");

        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("This account has been deactivated.");

        // Identity proof:
        //  - if a code is supplied, it MUST verify (TOTP or recovery)
        //  - else if a password is supplied AND 2FA is off AND purpose=Action,
        //    a password check is acceptable (lets non-2FA users still gate
        //    sensitive actions); never accepted for purpose=Admin.
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var verify = await _twoFactor.VerifyAsync(user, request.Code);
            if (verify.Outcome == TwoFactorVerifyOutcome.Locked)
            {
                throw new AccountLockedException(verify.RetryAfterSeconds ?? 900);
            }
            if (verify.Outcome != TwoFactorVerifyOutcome.Valid
                && verify.Outcome != TwoFactorVerifyOutcome.ValidViaRecoveryCode)
            {
                throw new UnauthorizedException("Invalid 2FA code.", reason: "invalid_credentials");
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Purpose == StepUpPurpose.Admin)
                throw new UnauthorizedException("Admin step-up requires a 2FA code.", reason: "invalid_credentials");

            if (user.TwoFactorEnabled)
                throw new UnauthorizedException("This account has 2FA — provide a code, not a password.", reason: "invalid_credentials");

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid password.", reason: "invalid_credentials");
        }
        else
        {
            throw new UnauthorizedException("Either a code or a password is required.", reason: "invalid_credentials");
        }

        var token = _tokenService.GenerateStepUpToken(user, request.Purpose);
        return new StepUpResponse(token, (int)request.Purpose.GetTtl().TotalSeconds);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new BadRequestException("Confirmation token is missing.", reason: "invalid_token");
        }

        var hash = HashToken(request.Token);

        // Two-step lookup so we can distinguish "unknown" from
        // "expired" / "already used" — the front needs to render
        // different copy for each.
        var token = await _confirmationTokens.GetAnyByHashAsync(hash);
        if (token is null)
        {
            throw new BadRequestException("Invalid confirmation token.", reason: "invalid_token");
        }

        if (token.ConsumedAt is not null)
        {
            // Idempotent-ish: if the user clicks the same link twice, the
            // second click should NOT 500. But we still want to signal
            // "this link was already used" so the front can offer "go to
            // login" rather than "resend".
            throw new BadRequestException("This confirmation link has already been used.", reason: "token_consumed");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BadRequestException("This confirmation link has expired. Request a new one.", reason: "token_expired");
        }

        // Mark consumed BEFORE flipping EmailConfirmed so a race between two
        // concurrent clicks can't double-flip (the second click finds
        // ConsumedAt set and throws).
        token.ConsumedAt = DateTime.UtcNow;
        await _confirmationTokens.UpdateAsync(token);

        var user = token.User
            ?? throw new InvalidOperationException(
                $"Confirmation token {token.Id} has no associated User — DB integrity issue.");

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userRepository.UpdateAsync(user);
        }

        _logger.LogInformation(
            "Email confirmed for user {UserId} ({Email}) via token {TokenId}.",
            user.Id, user.Email, token.Id);
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        // Anti-enumeration: always look like a success to the caller. Unknown
        // emails, confirmed accounts, throttled users all hit the same code
        // path that just returns. We log at info so ops can see the breakdown
        // without exposing it to clients.
        if (user is null)
        {
            _logger.LogInformation(
                "Resend-confirmation requested for unknown email {Email} — silent no-op.",
                normalizedEmail);
            return;
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation(
                "Resend-confirmation requested for already-confirmed user {UserId} — silent no-op.",
                user.Id);
            return;
        }

        var lastIssuedAt = await _confirmationTokens.GetMostRecentCreatedAtAsync(user.Id);
        if (lastIssuedAt is { } last && DateTime.UtcNow - last < ResendCooldown)
        {
            _logger.LogInformation(
                "Resend-confirmation throttled for user {UserId} — last issued {Seconds}s ago.",
                user.Id, (int)(DateTime.UtcNow - last).TotalSeconds);
            return;
        }

        var (rawToken, _) = await IssueConfirmationTokenAsync(user);
        try
        {
            await _confirmationSender.SendAsync(user, rawToken);
        }
        catch (Exception ex)
        {
            // Don't surface SMTP errors to the caller — that would be a
            // confirmation channel ("this email exists, our SMTP just broke").
            // Log and move on; the user will hit the resend button again.
            _logger.LogError(ex,
                "Resend-confirmation failed to send for user {UserId} ({Email}).",
                user.Id, user.Email);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        // Anti-enumeration: unknown emails, throttled users, and successful
        // sends all return the same 200 to the caller. Log so ops can see
        // the breakdown without exposing it.
        if (user is null)
        {
            _logger.LogInformation(
                "Forgot-password requested for unknown email {Email} — silent no-op.",
                normalizedEmail);
            return;
        }

        var lastIssuedAt = await _passwordResetTokens.GetMostRecentCreatedAtAsync(user.Id);
        if (lastIssuedAt is { } last && DateTime.UtcNow - last < PasswordResetCooldown)
        {
            _logger.LogInformation(
                "Forgot-password throttled for user {UserId} — last issued {Seconds}s ago.",
                user.Id, (int)(DateTime.UtcNow - last).TotalSeconds);
            return;
        }

        var (rawToken, _) = await IssuePasswordResetTokenAsync(user);
        try
        {
            await _passwordResetSender.SendAsync(user, rawToken);
        }
        catch (Exception ex)
        {
            // Don't surface SMTP errors to the caller — same anti-enumeration
            // concern as ResendConfirmationAsync.
            _logger.LogError(ex,
                "Forgot-password mail failed to send for user {UserId} ({Email}).",
                user.Id, user.Email);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new BadRequestException("Reset token is missing.", reason: "invalid_token");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            // Server-side defence — the FluentValidation rule already catches
            // this but a custom client could send a mismatched payload.
            throw new BadRequestException("Passwords do not match.", reason: "passwords_mismatch");
        }

        if (!_passwordHasher.MeetsRequirements(request.NewPassword, out _))
        {
            throw new BadRequestException(
                "Password does not meet requirements.", reason: "weak_password");
        }

        var hash = HashToken(request.Token);

        var token = await _passwordResetTokens.GetAnyByHashAsync(hash);
        if (token is null)
        {
            throw new BadRequestException("Invalid reset token.", reason: "invalid_token");
        }

        if (token.ConsumedAt is not null)
        {
            throw new BadRequestException(
                "This reset link has already been used.", reason: "token_consumed");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "This reset link has expired. Request a new one.", reason: "token_expired");
        }

        var user = token.User
            ?? throw new InvalidOperationException(
                $"Password reset token {token.Id} has no associated User — DB integrity issue.");

        // Mark consumed BEFORE rotating the password so a race between two
        // concurrent clicks can't double-flip (the second click finds
        // ConsumedAt set and throws).
        token.ConsumedAt = DateTime.UtcNow;
        await _passwordResetTokens.UpdateAsync(token);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        // Defensive: reset the per-account login-attempt counter so a user
        // who got locked out can come straight back in after resetting.
        await _loginAttempts.ResetAsync(user.Email);

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Password reset for user {UserId} ({Email}) via token {TokenId}.",
            user.Id, user.Email, token.Id);
    }

    /// <summary>
    /// Mirrors <see cref="IssueConfirmationTokenAsync"/> — fresh 32 random
    /// bytes → URL-safe base64 → SHA-256 hash stored, raw token returned for
    /// mailing. Lifetime comes from <see cref="PasswordResetOptions"/>.
    /// </summary>
    private async Task<(string Raw, string Hash)> IssuePasswordResetTokenAsync(User user)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = HashToken(raw);

        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.Add(_passwordResetOptions.TokenLifetime),
            CreatedAt = DateTime.UtcNow,
        };
        await _passwordResetTokens.CreateAsync(entity);
        return (raw, hash);
    }

    /// <summary>
    /// Generates a fresh single-use token, persists the SHA-256 hash, and
    /// returns the raw token (to mail) and the hash (for tests / logs).
    /// 32 random bytes → URL-safe base64 → 43-ish chars. Hash is hex (64 chars).
    /// </summary>
    private async Task<(string Raw, string Hash)> IssueConfirmationTokenAsync(User user)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = HashToken(raw);

        var entity = new EmailConfirmationToken
        {
            // Leave Id at default(Guid) — see Address / OrderStatusChange:
            // EF's heuristic marks default-PK navigation entries as Added.
            // Here we're adding via the repo (not via parent.Add) so it
            // doesn't strictly matter, but stay consistent with the pattern.
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.Add(_confirmationOptions.TokenLifetime),
            CreatedAt = DateTime.UtcNow,
        };
        await _confirmationTokens.CreateAsync(entity);

        return (raw, hash);
    }

    /// <summary>
    /// SHA-256 of the raw token's UTF-8 bytes, hex-encoded uppercase.
    /// </summary>
    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.Status,
        user.Anonymized,
        user.EmailConfirmed,
        user.TwoFactorEnabled,
        user.LastLogin,
        user.CreatedAt,
        user.Addresses.Select(a => new AddressDto(
            a.Id, a.Label, a.FirstName, a.LastName, a.Company,
            a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone
        )),
        user.PaymentMethods.Select(p => new PaymentMethodDto(
            p.Id, p.Type, p.Label,
            p.StripePaymentMethodId, p.Brand, p.Last4, p.ExpMonth, p.ExpYear)),
        user.CreditBalanceCents
    );
}
