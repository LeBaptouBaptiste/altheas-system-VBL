using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IStripeService> _stripe = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo.Object, _stripe.Object);
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
    public async Task AddAddressAsync_SameFingerprint_ReturnsExisting_NoInsert()
    {
        // Fix for the checkout duplicate-address bug: posting the same
        // (firstName + lastName + street + city + postalCode + country)
        // payload must NOT create a new row. Label is intentionally NOT
        // part of the fingerprint — "Facturation" vs "Livraison" pointing
        // at the same place is still one address from a logistics POV.
        var user = CreateTestUser();
        var existing = new Address
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Label = "Facturation",
            FirstName = "John",
            LastName = "Doe",
            Street = "1 rue de la Paix",
            City = "Paris",
            PostalCode = "75001",
            Country = "France",
        };
        user.Addresses.Add(existing);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var request = new AddressCreateRequest(
            "Livraison", // different label, same address
            "John", "Doe", null,
            "1 rue de la Paix", null,
            "Paris", "75001", "France", null);

        var result = await _sut.AddAddressAsync(user.Id, request);

        // Returns the existing id — no new row created.
        result.Id.Should().Be(existing.Id);
        user.Addresses.Should().HaveCount(1);
        // The label of the existing row is NOT updated by the dedup hit.
        result.Label.Should().Be("Facturation");
        // UpdateAsync NOT called because we short-circuited before inserting.
        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AddAddressAsync_WhitespaceVariation_StillDedups()
    {
        // Edge: paste artifacts ("  1 Rue   de  la Paix  ") should still
        // match the canonical entry. Verifies the NormalizeStreet helper.
        var user = CreateTestUser();
        user.Addresses.Add(new Address
        {
            Id = Guid.NewGuid(), UserId = user.Id, Label = "Home",
            FirstName = "Jane", LastName = "Doe",
            Street = "12 Rue de la Paix",
            City = "Paris", PostalCode = "75002", Country = "France",
        });
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var request = new AddressCreateRequest(
            "Billing", "Jane", "Doe", null,
            "  12 Rue   de la Paix  ", null,
            "Paris", "75002", "France", null);

        await _sut.AddAddressAsync(user.Id, request);

        user.Addresses.Should().HaveCount(1, "whitespace variations of the same street must dedupe");
    }

    [Fact]
    public async Task AddAddressAsync_DifferentStreet_InsertsAsExpected()
    {
        // Sanity: don't over-eagerly dedupe. Different street number / road
        // = different physical place → new row.
        var user = CreateTestUser();
        user.Addresses.Add(new Address
        {
            Id = Guid.NewGuid(), UserId = user.Id, Label = "A",
            FirstName = "X", LastName = "Y",
            Street = "1 rue", City = "Paris", PostalCode = "75001", Country = "France",
        });
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var request = new AddressCreateRequest(
            "B", "X", "Y", null,
            "2 rue", null, "Paris", "75001", "France", null);

        var result = await _sut.AddAddressAsync(user.Id, request);

        user.Addresses.Should().HaveCount(2);
        result.Street.Should().Be("2 rue");
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
