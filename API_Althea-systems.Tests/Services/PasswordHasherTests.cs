using FluentAssertions;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasherService _sut = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _sut.Hash("TestPassword1!");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_DifferentCallsProduceDifferentHashes()
    {
        var hash1 = _sut.Hash("TestPassword1!");
        var hash2 = _sut.Hash("TestPassword1!");
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("TestPassword1!");
        _sut.Verify("TestPassword1!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("TestPassword1!");
        _sut.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptyHash_ReturnsFalse()
    {
        _sut.Verify("anything", "").Should().BeFalse();
    }

    [Fact]
    public void MeetsRequirements_StrongPassword_ReturnsTrue()
    {
        var result = _sut.MeetsRequirements("StrongP@ss1", out var errors);
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void MeetsRequirements_TooShort_ReturnsFalse()
    {
        var result = _sut.MeetsRequirements("Aa1!", out var errors);
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("8 characters"));
    }

    [Fact]
    public void MeetsRequirements_NoUppercase_ReturnsFalse()
    {
        var result = _sut.MeetsRequirements("password1!", out var errors);
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("uppercase"));
    }

    [Fact]
    public void MeetsRequirements_NoDigit_ReturnsFalse()
    {
        var result = _sut.MeetsRequirements("Password!", out var errors);
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("digit"));
    }

    [Fact]
    public void MeetsRequirements_NoSpecialChar_ReturnsFalse()
    {
        var result = _sut.MeetsRequirements("Password1", out var errors);
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("special character"));
    }

    [Fact]
    public void MeetsRequirements_EmptyPassword_ReturnsFalse()
    {
        var result = _sut.MeetsRequirements("", out var errors);
        result.Should().BeFalse();
        errors.Should().Contain("Password is required.");
    }
}
