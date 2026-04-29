using System.Security.Cryptography;
using System.Text;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

/// <summary>
/// AES-256-GCM implementation of <see cref="IEncryptionService"/>.
///
/// Format (decoded base64):
///   [ 12 bytes nonce (IV) ][ N bytes ciphertext ][ 16 bytes tag ]
///
/// A fresh random nonce is drawn for every encryption — never reuse a
/// nonce with the same key. The 128-bit tag protects against tampering;
/// any modification to the stored value causes Decrypt to throw.
/// </summary>
public class EncryptionService : IEncryptionService
{
    // GCM standard sizes (RFC 5116 / NIST SP 800-38D).
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32; // 256-bit

    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException(
                "Encryption:Key is not configured. Generate a 32-byte random key " +
                "and provide it base64-encoded via Encryption:Key (or Encryption__Key env var).");

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "Encryption:Key must be a valid base64 string.", ex);
        }

        if (decoded.Length != KeySize)
            throw new InvalidOperationException(
                $"Encryption:Key must decode to {KeySize} bytes (256-bit AES key); got {decoded.Length}.");

        _key = decoded;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var output = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, output, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, output, NonceSize + cipherBytes.Length, TagSize);

        return Convert.ToBase64String(output);
    }

    public string Decrypt(string ciphertextBase64)
    {
        ArgumentNullException.ThrowIfNull(ciphertextBase64);

        byte[] data;
        try
        {
            data = Convert.FromBase64String(ciphertextBase64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Ciphertext is not valid base64.", ex);
        }

        if (data.Length < NonceSize + TagSize)
            throw new CryptographicException("Ciphertext is too short to contain nonce and tag.");

        var cipherLength = data.Length - NonceSize - TagSize;
        var nonce = new byte[NonceSize];
        var cipherBytes = new byte[cipherLength];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(data, NonceSize, cipherBytes, 0, cipherLength);
        Buffer.BlockCopy(data, NonceSize + cipherLength, tag, 0, TagSize);

        var plainBytes = new byte[cipherLength];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
