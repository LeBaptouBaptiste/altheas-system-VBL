namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Symmetric authenticated encryption for short secrets persisted at rest
/// (e.g. TOTP shared secret on User.TwoFactorSecret).
///
/// The output of <see cref="Encrypt"/> is a base64 string that bundles
/// a per-message random IV, the ciphertext, and an authentication tag,
/// so it can be stored as a single text column.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts the plaintext and returns base64(IV || ciphertext || tag).</summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a base64 payload produced by <see cref="Encrypt"/>.
    /// Throws <see cref="System.Security.Cryptography.CryptographicException"/>
    /// if the payload is malformed or has been tampered with.
    /// </summary>
    string Decrypt(string ciphertextBase64);
}
