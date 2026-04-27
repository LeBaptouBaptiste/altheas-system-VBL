using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ITwoFactorService> _twoFactor = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<bool>()))
                     .Returns("test-access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("test-refresh-token");
        _tokenService.Setup(t => t.GenerateChallengeToken(It.IsAny<User>())).Returns("test-challenge-token");
        _tokenService.Setup(t => t.GenerateAdminSetupToken(It.IsAny<User>())).Returns("test-setup-token");

        _sut = new AuthService(_userRepo.Object, _tokenService.Object, _twoFactor.Object);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsAuthResponse()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.RegisterAsync(new RegisterRequest("Test", "test@test.com", "Pass1234", "Pass1234"));

        result.AccessToken.Should().Be("test-access-token");
        result.User.Email.Should().Be("test@test.com");
        result.User.EmailConfirmed.Should().BeFalse();
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

    private static User CreateTestUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "test@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1234"),
        Role = UserRole.Customer,
        Status = UserStatus.Active,
        Addresses = [],
        PaymentMethods = []
    };
}
