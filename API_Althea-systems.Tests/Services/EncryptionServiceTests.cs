using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class EncryptionServiceTests
{
    private static EncryptionService BuildSut(string? keyBase64 = null)
    {
        keyBase64 ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = keyBase64
            })
            .Build();

        return new EncryptionService(config);
    }

    [Fact]
    public void Constructor_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder().Build();

        var act = () => new EncryptionService(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Encryption:Key is not configured*");
    }

    [Fact]
    public void Constructor_NonBase64Key_Throws()
    {
        var act = () => BuildSut("not-base64-!@#");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*valid base64*");
    }

    [Fact]
    public void Constructor_WrongKeySize_Throws()
    {
        // 16 bytes -> AES-128, refused (we mandate AES-256)
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        var act = () => BuildSut(shortKey);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*32 bytes*");
    }

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTripsPlaintext()
    {
        var sut = BuildSut();
        const string plaintext = "JBSWY3DPEHPK3PXP"; // looks like a base32 TOTP secret

        var cipher = sut.Encrypt(plaintext);
        var roundTrip = sut.Decrypt(cipher);

        roundTrip.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_TwoCalls_ProduceDifferentCiphertexts()
    {
        // GCM uses a fresh random nonce, so identical plaintexts must
        // never yield identical ciphertexts.
        var sut = BuildSut();

        var a = sut.Encrypt("same plaintext");
        var b = sut.Encrypt("same plaintext");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var sut = BuildSut();
        var cipher = sut.Encrypt("sensitive");

        // Flip the last byte (part of the GCM tag) -> auth must fail.
        var bytes = Convert.FromBase64String(cipher);
        bytes[^1] ^= 0x01;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => sut.Decrypt(tampered);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var sut1 = BuildSut();
        var sut2 = BuildSut(); // different random key

        var cipher = sut1.Encrypt("hello");

        var act = () => sut2.Decrypt(cipher);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_TooShortPayload_Throws()
    {
        var sut = BuildSut();
        // 10 bytes < 12 (nonce) + 16 (tag)
        var tooShort = Convert.ToBase64String(new byte[10]);

        var act = () => sut.Decrypt(tooShort);
        act.Should().Throw<CryptographicException>()
           .WithMessage("*too short*");
    }

    [Fact]
    public void Decrypt_NotBase64_Throws()
    {
        var sut = BuildSut();

        var act = () => sut.Decrypt("not-base64-!!!");
        act.Should().Throw<CryptographicException>()
           .WithMessage("*valid base64*");
    }

    [Fact]
    public void Encrypt_EmptyString_RoundTrips()
    {
        var sut = BuildSut();

        var cipher = sut.Encrypt("");
        var roundTrip = sut.Decrypt(cipher);

        roundTrip.Should().Be("");
    }
}
