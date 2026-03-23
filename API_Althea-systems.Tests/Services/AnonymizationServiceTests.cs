using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class AnonymizationServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly AnonymizationService _sut;

    public AnonymizationServiceTests()
    {
        _sut = new AnonymizationService(_userRepo.Object);
    }

    [Fact]
    public async Task AnonymizeUserAsync_ValidUser_AnonymizesAllData()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Sophie Martin",
            Email = "sophie@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer,
            Status = UserStatus.Active,
            Anonymized = false,
            TwoFactorSecret = "secret",
            TwoFactorEnabled = true,
            Addresses = new List<Address>
            {
                new() { Id = Guid.NewGuid(), Label = "Home", FirstName = "Sophie", LastName = "Martin", Street = "1 rue", City = "Lyon", PostalCode = "69000", Country = "FR" }
            },
            PaymentMethods = new List<UserPaymentMethod>
            {
                new() { Id = Guid.NewGuid(), Type = "visa", Label = "Visa 4242" }
            }
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.AnonymizeUserAsync(user.Id);

        // Personal data cleared
        result.Name.Should().Be("Utilisateur Anonymisé");
        result.Email.Should().Contain("@deleted.local");
        result.Anonymized.Should().BeTrue();
        result.Status.Should().Be(UserStatus.Inactive);
        result.TwoFactorEnabled.Should().BeFalse();

        // Original object modified
        user.PasswordHash.Should().BeEmpty();
        user.TwoFactorSecret.Should().BeNull();
        user.Addresses.Should().BeEmpty();
        user.PaymentMethods.Should().BeEmpty();

        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task AnonymizeUserAsync_AlreadyAnonymized_ThrowsConflict()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Name = "Anon", Email = "anon@deleted.local",
            PasswordHash = "", Anonymized = true, Status = UserStatus.Inactive,
            Addresses = [], PaymentMethods = []
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.AnonymizeUserAsync(user.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AnonymizeUserAsync_NonExistentUser_ThrowsNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var act = () => _sut.AnonymizeUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void IsAnonymized_AnonymizedUser_ReturnsTrue()
    {
        var user = new User { Anonymized = true, Addresses = [], PaymentMethods = [] };
        _sut.IsAnonymized(user).Should().BeTrue();
    }

    [Fact]
    public void IsAnonymized_NormalUser_ReturnsFalse()
    {
        var user = new User { Anonymized = false, Addresses = [], PaymentMethods = [] };
        _sut.IsAnonymized(user).Should().BeFalse();
    }
}
