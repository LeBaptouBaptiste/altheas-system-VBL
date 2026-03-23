using FluentAssertions;
using Moq;
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
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("test-access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("test-refresh-token");
        _sut = new AuthService(_userRepo.Object, _tokenService.Object);
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

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "Password1234"));

        result.AccessToken.Should().Be("test-access-token");
        result.User.Email.Should().Be("test@test.com");
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once); // LastLogin update
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

    [Fact]
    public async Task Verify2FaAsync_CorrectCode_EnablesTwoFactor()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await _sut.Verify2FaAsync(user.Id, new Verify2FaRequest("123456"));

        user.TwoFactorEnabled.Should().BeTrue();
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Verify2FaAsync_WrongCode_ThrowsUnauthorized()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.Verify2FaAsync(user.Id, new Verify2FaRequest("000000"));

        await act.Should().ThrowAsync<UnauthorizedException>();
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
