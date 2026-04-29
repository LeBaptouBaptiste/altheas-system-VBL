using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITwoFactorService _twoFactor;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ITwoFactorService twoFactor,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _twoFactor = twoFactor;
        _passwordHasher = passwordHasher;
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
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
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedException("Invalid email or password.", reason: "invalid_credentials");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.", reason: "invalid_credentials");

        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("This account has been deactivated.");

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
        // In production, validate the token from an email service
        // For now, we confirm based on a simple token lookup
        var user = await _userRepository.GetByEmailAsync(request.Token)
            ?? throw new NotFoundException("User", request.Token);

        user.EmailConfirmed = true;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant());
        // Always return success to prevent email enumeration
        if (user == null) return;

        // In production: generate a reset token and send via email service
        // For now, this is a no-op stub
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        // In production: validate the reset token
        // For now, we use the token as email for simplicity
        var user = await _userRepository.GetByEmailAsync(request.Token)
            ?? throw new UnauthorizedException("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);
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
        user.PaymentMethods.Select(p => new PaymentMethodDto(p.Id, p.Type, p.Label))
    );
}
