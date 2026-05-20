using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _confirmationTokens = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ITwoFactorService> _twoFactor = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ILoginAttemptStore> _loginAttempts = new();
    private readonly Mock<IEmailConfirmationSender> _confirmationSender = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<bool>()))
                     .Returns("test-access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("test-refresh-token");
        _tokenService.Setup(t => t.GenerateChallengeToken(It.IsAny<User>())).Returns("test-challenge-token");
        _tokenService.Setup(t => t.GenerateAdminSetupToken(It.IsAny<User>())).Returns("test-setup-token");
        _tokenService.Setup(t => t.GenerateStepUpToken(It.IsAny<User>(), It.IsAny<StepUpPurpose>()))
                     .Returns("test-stepup-token");

        // Default: account is never locked, increments are no-ops. Individual
        // tests override these to exercise the lockout logic.
        _loginAttempts.Setup(l => l.GetLockRemainingAsync(It.IsAny<string>()))
                      .ReturnsAsync((TimeSpan?)null);
        _loginAttempts.Setup(l => l.IncrementFailureAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                      .ReturnsAsync(1);

        // Default: password verification succeeds. Tests that exercise the
        // failure branch override this with .Setup(...).Returns(false).
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => $"hashed:{p}");

        // Email-confirmation defaults: no prior token (resend not throttled),
        // confirmation send is a no-op so the happy path doesn't blow up.
        _confirmationTokens.Setup(r => r.GetMostRecentCreatedAtAsync(It.IsAny<Guid>()))
                           .ReturnsAsync((DateTime?)null);

        var confirmationOptions = Options.Create(new EmailConfirmationOptions
        {
            FrontendBaseUrl = "http://localhost:3000",
            TokenLifetime = TimeSpan.FromHours(24),
        });

        _sut = new AuthService(
            _userRepo.Object,
            _confirmationTokens.Object,
            _tokenService.Object,
            _twoFactor.Object,
            _hasher.Object,
            _loginAttempts.Object,
            _confirmationSender.Object,
            confirmationOptions,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsRegisterResponse_WithoutTokens()
    {
        // Phase 2: register no longer logs the user in. The user must
        // confirm their email first → no AccessToken in the response.
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.RegisterAsync(new RegisterRequest("Test", "test@test.com", "Pass1234", "Pass1234"));

        result.User.Email.Should().Be("test@test.com");
        result.User.EmailConfirmed.Should().BeFalse();
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
        // No access token issuance — guarantee the user can't bypass confirmation.
        _tokenService.Verify(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_PersistsConfirmationToken_AndSendsEmail()
    {
        // The token entity must end up in the repo; the email must be sent.
        // Two independent assertions because a bug in one shouldn't mask
        // a bug in the other.
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(new RegisterRequest("Test", "test@test.com", "Pass1234", "Pass1234"));

        _confirmationTokens.Verify(r => r.CreateAsync(It.Is<EmailConfirmationToken>(t =>
            t.TokenHash.Length == 64 // hex SHA-256
            && t.ConsumedAt == null
            && t.ExpiresAt > DateTime.UtcNow)), Times.Once);
        _confirmationSender.Verify(s => s.SendAsync(
            It.Is<User>(u => u.Email == "test@test.com"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailSendFails_StillCreatesUser()
    {
        // A flaky SMTP MUST NOT roll back the registration — the user can
        // resend the confirmation email from the front. Otherwise a paying
        // customer whose mailbox is full would be unable to register.
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _confirmationSender
            .Setup(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception("boom")));

        var act = () => _sut.RegisterAsync(new RegisterRequest("Test", "x@x.com", "Pass1234", "Pass1234"));

        await act.Should().NotThrowAsync();
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsConflict()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("existing@test.com")).ReturnsAsync(true);

        var act = () => _sut.RegisterAsync(new RegisterRequest("Test", "existing@test.com", "Pass1234", "Pass1234"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─────────────────────────────────────────────────────────
    //  Login — three branches
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_RegularUserWithoutTwoFactor_ReturnsAuthenticated()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.Authenticated);
        result.Auth.Should().NotBeNull();
        result.Auth!.AccessToken.Should().Be("test-access-token");
        result.Auth.User.Email.Should().Be("test@test.com");
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once); // LastLogin update
    }

    [Fact]
    public async Task LoginAsync_RegularUserWithoutTwoFactor_GeneratesAmrPwdOnly()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        _tokenService.Verify(t => t.GenerateAccessToken(user, false), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_TwoFactorEmail_FiresLoginCodeBeforeReturningChallenge()
    {
        // Phase 4b: for Method=Email users, AuthService must request a fresh
        // 6-digit code from TwoFactorService before returning the challenge,
        // so the code is in the user's inbox by the time the challenge UI
        // shows up.
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = Common.Enums.TwoFactorMethod.Email;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.TwoFactorRequired);
        result.ChallengeToken.Should().Be("test-challenge-token");
        _twoFactor.Verify(t => t.RequestLoginEmailCodeAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_TwoFactorEmail_CodeSendFails_StillReturnsChallenge()
    {
        // SMTP failure must NOT block the challenge — the user can hit
        // /auth/2fa/resend-code to retry. Otherwise a flaky inbox = unable
        // to log in.
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = Common.Enums.TwoFactorMethod.Email;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _twoFactor
            .Setup(t => t.RequestLoginEmailCodeAsync(user))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.TwoFactorRequired);
        result.ChallengeToken.Should().Be("test-challenge-token");
    }

    [Fact]
    public async Task LoginAsync_TwoFactorEnabled_ReturnsChallengeToken_AndDoesNotUpdateLastLogin()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.TwoFactorRequired);
        result.ChallengeToken.Should().Be("test-challenge-token");
        result.Auth.Should().BeNull();
        result.SetupToken.Should().BeNull();
        // LastLogin must wait until verify succeeds
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_AdminWithoutTwoFactor_ReturnsSetupToken()
    {
        var user = CreateTestUser();
        user.Role = UserRole.Admin;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.TwoFactorSetupRequired);
        result.SetupToken.Should().Be("test-setup-token");
        result.Auth.Should().BeNull();
        result.ChallengeToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_AdminWithTwoFactorEnabled_ReturnsChallengeToken()
    {
        // Edge: an admin who already has 2FA goes through the normal challenge flow.
        var user = CreateTestUser();
        user.Role = UserRole.Admin;
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.TwoFactorRequired);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("WrongPassword", user.PasswordHash)).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("test@test.com", "WrongPassword"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nobody@test.com", "anything"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ─────────────────────────────────────────────────────────
    //  Per-account login throttling
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_AccountLocked_ThrowsAccountLocked_BeforeUserLookup()
    {
        _loginAttempts.Setup(l => l.GetLockRemainingAsync("test@test.com"))
                      .ReturnsAsync(TimeSpan.FromSeconds(120));

        var act = () => _sut.LoginAsync(new LoginRequest("test@test.com", "anything"));

        var ex = await act.Should().ThrowAsync<AccountLockedException>();
        ex.And.RetryAfterSeconds.Should().BeGreaterThan(0);
        // Crucial: we must NOT hit the user repo when the account is locked
        // — otherwise an attacker can use timing to fingerprint accounts.
        _userRepo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailureCounter()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Wrong", user.PasswordHash)).Returns(false);
        _loginAttempts.Setup(l => l.IncrementFailureAsync("test@test.com", It.IsAny<TimeSpan>()))
                      .ReturnsAsync(1);

        var act = () => _sut.LoginAsync(new LoginRequest("test@test.com", "Wrong"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _loginAttempts.Verify(l => l.IncrementFailureAsync("test@test.com", It.IsAny<TimeSpan>()), Times.Once);
        _loginAttempts.Verify(l => l.LockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_FifthFailure_LocksAccount()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync((User?)null);
        _loginAttempts.Setup(l => l.IncrementFailureAsync("test@test.com", It.IsAny<TimeSpan>()))
                      .ReturnsAsync(5);

        var act = () => _sut.LoginAsync(new LoginRequest("test@test.com", "Wrong"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _loginAttempts.Verify(l => l.LockAsync("test@test.com", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Success_ResetsFailureCounter()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        _loginAttempts.Verify(l => l.ResetAsync("test@test.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_StillIncrementsFailure()
    {
        // Important so that an attacker enumerating emails ALSO hits the
        // lockout (otherwise mass-checking emails is unbounded).
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nobody@test.com", "anything"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _loginAttempts.Verify(l => l.IncrementFailureAsync("nobody@test.com", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsForbidden()
    {
        var user = CreateTestUser();
        user.Status = UserStatus.Inactive;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─────────────────────────────────────────────────────────
    //  CompleteTwoFactorChallengeAsync
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteTwoFactorChallengeAsync_ValidTotp_ReturnsAuthResponseWithMfaAmr()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _tokenService.Setup(t => t.ValidateSpecialToken("the-challenge", TokenPurpose.TwoFactorChallenge))
                     .Returns(user.Id);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, "123456"))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Valid));

        var result = await _sut.CompleteTwoFactorChallengeAsync(
            new VerifyTwoFactorChallengeRequest("the-challenge", "123456"));

        result.AccessToken.Should().Be("test-access-token");
        result.User.Email.Should().Be("test@test.com");
        // Crucial: token issued with mfaVerified=true.
        _tokenService.Verify(t => t.GenerateAccessToken(user, true), Times.Once);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once); // LastLogin
    }

    [Fact]
    public async Task CompleteTwoFactorChallengeAsync_RecoveryCodePath_AlsoReturnsAuthResponse()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _tokenService.Setup(t => t.ValidateSpecialToken("c", TokenPurpose.TwoFactorChallenge)).Returns(user.Id);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, "abcd-1234-ef56-7890"))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.ValidViaRecoveryCode));

        var result = await _sut.CompleteTwoFactorChallengeAsync(
            new VerifyTwoFactorChallengeRequest("c", "abcd-1234-ef56-7890"));

        result.Should().NotBeNull();
        _tokenService.Verify(t => t.GenerateAccessToken(user, true), Times.Once);
    }

    [Fact]
    public async Task CompleteTwoFactorChallengeAsync_InvalidChallengeToken_ThrowsUnauthorized()
    {
        _tokenService.Setup(t => t.ValidateSpecialToken(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns((Guid?)null);

        var act = () => _sut.CompleteTwoFactorChallengeAsync(
            new VerifyTwoFactorChallengeRequest("expired", "123456"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task CompleteTwoFactorChallengeAsync_InvalidCode_ThrowsUnauthorized()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _tokenService.Setup(t => t.ValidateSpecialToken("c", TokenPurpose.TwoFactorChallenge)).Returns(user.Id);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, It.IsAny<string>()))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Invalid));

        var act = () => _sut.CompleteTwoFactorChallengeAsync(
            new VerifyTwoFactorChallengeRequest("c", "000000"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTwoFactorChallengeAsync_InactiveUser_ThrowsForbidden()
    {
        var user = CreateTestUser();
        user.Status = UserStatus.Inactive;
        user.TwoFactorEnabled = true;
        _tokenService.Setup(t => t.ValidateSpecialToken("c", TokenPurpose.TwoFactorChallenge)).Returns(user.Id);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.CompleteTwoFactorChallengeAsync(
            new VerifyTwoFactorChallengeRequest("c", "123456"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─────────────────────────────────────────────────────────
    //  Existing assertions
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsDto()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetCurrentUserAsync(user.Id);

        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_NonExistent_ThrowsNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = () => _sut.GetCurrentUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────────────
    //  IssueTokensAsync (helper used by Login + /2fa/enable + /2fa/verify)
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task IssueTokensAsync_UpdatesLastLoginAndReturnsTokens()
    {
        var user = CreateTestUser();

        var result = await _sut.IssueTokensAsync(user, mfaVerified: true);

        result.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.User.Email.Should().Be("test@test.com");
        user.LastLogin.Should().NotBeNull();
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        _tokenService.Verify(t => t.GenerateAccessToken(user, true), Times.Once);
    }

    [Fact]
    public async Task IssueTokensAsync_PropagatesMfaFlagToTokenService()
    {
        var user = CreateTestUser();

        await _sut.IssueTokensAsync(user, mfaVerified: false);

        _tokenService.Verify(t => t.GenerateAccessToken(user, false), Times.Once);
    }

    // ─────────────────────────────────────────────────────────
    //  StepUpAsync
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StepUpAsync_TwoFactorOn_ValidCode_ReturnsToken()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, "123456"))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Valid));

        var result = await _sut.StepUpAsync(user.Id, new StepUpRequest(StepUpPurpose.Action, Code: "123456"));

        result.Token.Should().Be("test-stepup-token");
        result.ExpiresIn.Should().Be(60);
        _tokenService.Verify(t => t.GenerateStepUpToken(user, StepUpPurpose.Action), Times.Once);
    }

    [Fact]
    public async Task StepUpAsync_TwoFactorOn_RecoveryCodeAccepted()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, "abcd-1234-ef56-7890"))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.ValidViaRecoveryCode));

        var result = await _sut.StepUpAsync(user.Id,
            new StepUpRequest(StepUpPurpose.Action, Code: "abcd-1234-ef56-7890"));

        result.Token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task StepUpAsync_AdminPurpose_TtlIs30Minutes()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, It.IsAny<string>()))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Valid));

        var result = await _sut.StepUpAsync(user.Id, new StepUpRequest(StepUpPurpose.Admin, Code: "123456"));

        result.ExpiresIn.Should().Be(30 * 60);
    }

    [Fact]
    public async Task StepUpAsync_TwoFactorOff_PasswordAndActionPurpose_ReturnsToken()
    {
        var user = CreateTestUser(); // 2FA off
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Password1234", user.PasswordHash)).Returns(true);

        var result = await _sut.StepUpAsync(user.Id,
            new StepUpRequest(StepUpPurpose.Action, Password: "Password1234"));

        result.Token.Should().Be("test-stepup-token");
    }

    [Fact]
    public async Task StepUpAsync_TwoFactorOff_PasswordAndAdminPurpose_Throws()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.StepUpAsync(user.Id,
            new StepUpRequest(StepUpPurpose.Admin, Password: "Password1234"));

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("*Admin step-up requires a 2FA code*");
    }

    [Fact]
    public async Task StepUpAsync_TwoFactorOn_PasswordOnly_Throws()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.StepUpAsync(user.Id,
            new StepUpRequest(StepUpPurpose.Action, Password: "Password1234"));

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("*provide a code*");
    }

    [Fact]
    public async Task StepUpAsync_InvalidCode_Throws()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _twoFactor.Setup(t => t.VerifyAsync(user, It.IsAny<string>()))
                  .ReturnsAsync(new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Invalid));

        var act = () => _sut.StepUpAsync(user.Id, new StepUpRequest(StepUpPurpose.Action, Code: "000000"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task StepUpAsync_WrongPassword_Throws()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var act = () => _sut.StepUpAsync(user.Id,
            new StepUpRequest(StepUpPurpose.Action, Password: "WrongPassword"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task StepUpAsync_NeitherCodeNorPassword_Throws()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.StepUpAsync(user.Id, new StepUpRequest(StepUpPurpose.Action));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task StepUpAsync_UnknownUser_Throws()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = () => _sut.StepUpAsync(Guid.NewGuid(),
            new StepUpRequest(StepUpPurpose.Action, Code: "123456"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task StepUpAsync_InactiveUser_ThrowsForbidden()
    {
        var user = CreateTestUser();
        user.Status = UserStatus.Inactive;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.StepUpAsync(user.Id, new StepUpRequest(StepUpPurpose.Action, Code: "123456"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─────────────────────────────────────────────────────────
    //  Login — email confirmation gate (phase 2)
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_UnconfirmedEmail_ReturnsEmailConfirmationRequired_NoTokens()
    {
        // Password is correct but EmailConfirmed=false → block token issuance.
        // The front shows a "check your inbox" screen.
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.EmailConfirmationRequired);
        result.Auth.Should().BeNull();
        result.ChallengeToken.Should().BeNull();
        result.SetupToken.Should().BeNull();
        // No access token must be issued for an unconfirmed user.
        _tokenService.Verify(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        // LastLogin must NOT be touched (the login isn't complete).
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnconfirmedEmail_TakesPriorityOverTwoFactor()
    {
        // Defensive: even if the user somehow has TwoFactorEnabled=true
        // without confirming (shouldn't be reachable through the UI, but
        // a DB-direct edit could), the confirmation gate fires first.
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        user.TwoFactorEnabled = true;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.Outcome.Should().Be(LoginOutcome.EmailConfirmationRequired);
        // No challenge token issued — the 2FA branch is unreachable.
        _tokenService.Verify(t => t.GenerateChallengeToken(It.IsAny<User>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────
    //  ConfirmEmailAsync
    // ─────────────────────────────────────────────────────────

    private static string Sha256Hex(string raw) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_FlipsEmailConfirmed_AndMarksConsumed()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        var raw = "fresh-token";
        var hash = Sha256Hex(raw);
        var token = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        _confirmationTokens.Setup(r => r.GetAnyByHashAsync(hash)).ReturnsAsync(token);

        await _sut.ConfirmEmailAsync(new ConfirmEmailRequest(raw));

        token.ConsumedAt.Should().NotBeNull();
        user.EmailConfirmed.Should().BeTrue();
        _confirmationTokens.Verify(r => r.UpdateAsync(token), Times.Once);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_UnknownToken_ThrowsInvalidToken()
    {
        _confirmationTokens.Setup(r => r.GetAnyByHashAsync(It.IsAny<string>()))
                           .ReturnsAsync((EmailConfirmationToken?)null);

        var act = () => _sut.ConfirmEmailAsync(new ConfirmEmailRequest("bogus"));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("invalid_token");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ConsumedToken_ThrowsTokenConsumed()
    {
        // Double-click on the link → second click sees a populated ConsumedAt.
        // We surface a distinct reason so the front can offer "Go to login"
        // rather than "Request a new link".
        var user = CreateTestUser();
        user.EmailConfirmed = true;
        var raw = "already-used";
        var token = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = Sha256Hex(raw),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            ConsumedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        _confirmationTokens.Setup(r => r.GetAnyByHashAsync(token.TokenHash)).ReturnsAsync(token);

        var act = () => _sut.ConfirmEmailAsync(new ConfirmEmailRequest(raw));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("token_consumed");
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ExpiredToken_ThrowsTokenExpired()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        var raw = "stale";
        var token = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = Sha256Hex(raw),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        };
        _confirmationTokens.Setup(r => r.GetAnyByHashAsync(token.TokenHash)).ReturnsAsync(token);

        var act = () => _sut.ConfirmEmailAsync(new ConfirmEmailRequest(raw));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("token_expired");
        user.EmailConfirmed.Should().BeFalse("expired tokens must NOT confirm the email");
    }

    [Fact]
    public async Task ConfirmEmailAsync_BlankToken_ThrowsInvalidToken()
    {
        // Defensive: validator should catch this, but the service must also
        // guard so we don't issue an Sha256 of "" and look up a non-empty hash.
        var act = () => _sut.ConfirmEmailAsync(new ConfirmEmailRequest("   "));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("invalid_token");
    }

    // ─────────────────────────────────────────────────────────
    //  ResendConfirmationAsync
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendConfirmationAsync_UnknownEmail_IsSilentNoOp()
    {
        // Anti-enumeration: unknown emails must look indistinguishable from
        // known unconfirmed ones from the caller's perspective. We assert
        // the SMTP path is NOT touched, so an attacker can't even use
        // timing differences.
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await _sut.ResendConfirmationAsync(new ResendConfirmationRequest("nobody@x.com"));

        _confirmationSender.Verify(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _confirmationTokens.Verify(r => r.CreateAsync(It.IsAny<EmailConfirmationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationAsync_AlreadyConfirmedUser_IsSilentNoOp()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = true;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        await _sut.ResendConfirmationAsync(new ResendConfirmationRequest("test@test.com"));

        _confirmationSender.Verify(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationAsync_WithinCooldown_IsSilentNoOp()
    {
        // Last token was issued 1 min ago → cooldown is 5 min → skip.
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _confirmationTokens.Setup(r => r.GetMostRecentCreatedAtAsync(user.Id))
                           .ReturnsAsync(DateTime.UtcNow.AddMinutes(-1));

        await _sut.ResendConfirmationAsync(new ResendConfirmationRequest("test@test.com"));

        _confirmationSender.Verify(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _confirmationTokens.Verify(r => r.CreateAsync(It.IsAny<EmailConfirmationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationAsync_UnconfirmedUser_OutsideCooldown_IssuesNewToken()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _confirmationTokens.Setup(r => r.GetMostRecentCreatedAtAsync(user.Id))
                           .ReturnsAsync(DateTime.UtcNow.AddHours(-1)); // outside the 5 min cooldown

        await _sut.ResendConfirmationAsync(new ResendConfirmationRequest("test@test.com"));

        _confirmationTokens.Verify(r => r.CreateAsync(It.IsAny<EmailConfirmationToken>()), Times.Once);
        _confirmationSender.Verify(s => s.SendAsync(user, It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationAsync_EmailSendFails_StillSucceedsSilently()
    {
        // SMTP failure must not surface to the caller — that would be an
        // enumeration oracle ("emails that fail to send exist").
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _confirmationSender
            .Setup(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));

        var act = () => _sut.ResendConfirmationAsync(new ResendConfirmationRequest("test@test.com"));

        await act.Should().NotThrowAsync();
    }

    private static User CreateTestUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "test@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1234"),
        Role = UserRole.Customer,
        Status = UserStatus.Active,
        // Most tests pre-phase-2 assumed EmailConfirmed was a no-op — keep
        // that by defaulting to true here. The dedicated unconfirmed-login
        // test sets it back to false explicitly.
        EmailConfirmed = true,
        Addresses = [],
        PaymentMethods = []
    };
}
