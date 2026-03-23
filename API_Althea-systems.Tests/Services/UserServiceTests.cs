using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsDto()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.UpdateAsync(user.Id, new UserUpdateRequest("New Name", null, null));

        result.Name.Should().Be("New Name");
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SetsUserInactive()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await _sut.DeleteAsync(user.Id);

        user.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public async Task AddAddressAsync_AddsToUser()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var request = new AddressCreateRequest("Work", "John", "Doe", null, "1 rue", null, "Paris", "75001", "France", null);
        var result = await _sut.AddAddressAsync(user.Id, request);

        result.Label.Should().Be("Work");
        user.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAddressAsync_RemovesAddress()
    {
        var user = CreateTestUser();
        var address = new Address { Id = Guid.NewGuid(), Label = "Home", FirstName = "A", LastName = "B", Street = "1", City = "C", PostalCode = "1", Country = "F" };
        user.Addresses.Add(address);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await _sut.DeleteAddressAsync(user.Id, address.Id);

        user.Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAddressAsync_NonExistentAddress_ThrowsNotFound()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var act = () => _sut.DeleteAddressAsync(user.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static User CreateTestUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "test@test.com",
        PasswordHash = "hash",
        Role = UserRole.Customer,
        Status = UserStatus.Active,
        Addresses = new List<Address>(),
        PaymentMethods = new List<UserPaymentMethod>()
    };
}
