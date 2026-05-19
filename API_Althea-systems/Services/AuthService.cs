using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class AuthService : IAuthService
{
    // Per-account brute-force protection: 5 failures within a 15 min window
    // trigger a 15 min lockout. The IP-based "auth" rate-limiter remains in
    // place as defence in depth; this complements it for distributed attacks.
    private const int MaxFailuresBeforeLock = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITwoFactorService _twoFactor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginAttemptStore _loginAttempts;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ITwoFactorService twoFactor,
        IPasswordHasher passwordHasher,
        ILoginAttemptStore loginAttempts)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _twoFactor = twoFactor;
        _passwordHasher = passwordHasher;
        _loginAttempts = loginAttempts;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
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

        // New users never have 2FA at register time, so amr=pwd is correct.
        var accessToken = _tokenService.GenerateAccessToken(user, mfaVerified: false);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse(accessToken, refreshToken, MapToDto(user));
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

        // Branch 1: 2FA enabled -> issue a challenge, do NOT update LastLogin yet
        // (that happens once the code is verified).
        if (user.TwoFactorEnabled)
        {
            var challenge = _tokenService.GenerateChallengeToken(user);
            return new LoginResponse(LoginOutcome.TwoFactorRequired, ChallengeToken: challenge);
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

    /// <summary>
    /// DISABLED — the previous implementation treated the user's email as
    /// the confirmation token, which allowed a trivial account take-over
    /// (any caller who knew an email could mark it as verified).
    /// Re-enable only after a signed, single-use token table is added
    /// (EmailConfirmationTokens, 30 min TTL). The controller short-circuits
    /// to 501 so this method is currently unreachable from HTTP.
    /// TODO: implement signed single-use token table (EmailConfirmationTokens)
    ///       with 30 min TTL.
    /// </summary>
    [Obsolete("Disabled: insecure (email-as-token). Awaiting signed token implementation.", error: false)]
    public Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        throw new NotSupportedException(
            "ConfirmEmail is disabled until a signed single-use token table is implemented.");
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant());
        // Always return success to prevent email enumeration
        if (user == null) return;

        // In production: generate a reset token and send via email service
        // For now, this is a no-op stub
    }

    /// <summary>
    /// DISABLED — the previous implementation accepted the user's email as
    /// the reset token, which allowed any attacker who knew an email to
    /// reset the password and take over the account.
    /// Re-enable only after a signed, single-use token table is added
    /// (PasswordResetTokens, 30 min TTL). The controller short-circuits to
    /// 501 so this method is currently unreachable from HTTP.
    /// TODO: implement signed single-use token table (PasswordResetTokens)
    ///       with 30 min TTL.
    /// </summary>
    [Obsolete("Disabled: insecure (email-as-token). Awaiting signed token implementation.", error: false)]
    public Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        throw new NotSupportedException(
            "ResetPassword is disabled until a signed single-use token table is implemented.");
    }

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
            p.StripePaymentMethodId, p.Brand, p.Last4, p.ExpMonth, p.ExpYear))
    );
}
