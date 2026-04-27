using System.Security.Cryptography;
using OtpNet;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class TwoFactorService : ITwoFactorService
{
    // ── Constants (could move to IOptions later) ─────────────
    private const string Issuer = "AltheaSystems";
    private const int SecretBytes = 20;            // 160-bit TOTP secret (RFC 4226 recommendation)
    private const int RecoveryCodeCount = 10;
    private const int TotpStepSeconds = 30;
    private const int TotpDigits = 6;
    private static readonly TimeSpan SetupTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ReplayGuardTtl = TimeSpan.FromSeconds(90);

    // Recovery codes: 4 groups of 4 hex chars => 16 hex chars => 64 bits of entropy.
    private const int RecoveryGroups = 4;
    private const int RecoveryGroupLength = 4;
    private const string RecoveryAlphabet = "0123456789abcdef";

    private readonly ITwoFactorStateStore _state;
    private readonly IUserRepository _users;
    private readonly IRecoveryCodeRepository _recoveryCodes;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        ITwoFactorStateStore state,
        IUserRepository users,
        IRecoveryCodeRepository recoveryCodes,
        IEncryptionService encryption,
        IPasswordHasher hasher,
        ILogger<TwoFactorService> logger)
    {
        _state = state;
        _users = users;
        _recoveryCodes = recoveryCodes;
        _encryption = encryption;
        _hasher = hasher;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    //  Setup
    // ─────────────────────────────────────────────────────────

    public async Task<TwoFactorSetupResult> StartSetupAsync(User user)
    {
        var secretBytes = RandomNumberGenerator.GetBytes(SecretBytes);
        var base32Secret = Base32Encoding.ToString(secretBytes).TrimEnd('=');

        await _state.SetSetupSecretAsync(user.Id, base32Secret, SetupTtl);

        return new TwoFactorSetupResult(base32Secret, BuildOtpAuthUri(user.Email, base32Secret));
    }

    public async Task<TwoFactorEnableResult> EnableAsync(User user, string code)
    {
        var pending = await _state.GetSetupSecretAsync(user.Id)
            ?? throw new InvalidOperationException(
                "No pending 2FA setup. Call StartSetupAsync first (or it has expired).");

        if (!VerifyTotpCode(pending, code, out _))
        {
            _logger.LogWarning("2FA enable failed for user {UserId}: invalid TOTP", user.Id);
            throw new InvalidOperationException("Invalid TOTP code.");
        }

        // Persist the encrypted secret on the user.
        user.TwoFactorSecret = _encryption.Encrypt(pending);
        user.TwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        // Wipe pending and any stale recovery codes.
        await _state.DeleteSetupSecretAsync(user.Id);
        await _recoveryCodes.DeleteAllForUserAsync(user.Id);

        var (plaintextCodes, hashedEntities) = GenerateRecoveryCodes(user.Id);
        await _recoveryCodes.AddRangeAsync(hashedEntities);

        _logger.LogInformation("2FA enabled for user {UserId}", user.Id);
        return new TwoFactorEnableResult(plaintextCodes);
    }

    // ─────────────────────────────────────────────────────────
    //  Verify
    // ─────────────────────────────────────────────────────────

    public async Task<TwoFactorVerifyResult> VerifyAsync(User user, string codeOrRecovery)
    {
        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.NotEnabled);

        var normalized = (codeOrRecovery ?? string.Empty).Trim();

        // Recovery code path: detect by length / dashes.
        if (LooksLikeRecoveryCode(normalized))
        {
            var ok = await VerifyRecoveryCodeAsync(user.Id, normalized);
            return new TwoFactorVerifyResult(
                ok ? TwoFactorVerifyOutcome.ValidViaRecoveryCode : TwoFactorVerifyOutcome.Invalid);
        }

        // TOTP path: 6 digits.
        var base32 = _encryption.Decrypt(user.TwoFactorSecret);
        if (!VerifyTotpCode(base32, normalized, out var matchedStep))
            return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Invalid);

        // Replay protection: refuse if this step was already consumed.
        var prev = await _state.GetLastReplayStepAsync(user.Id);
        if (prev.HasValue && matchedStep <= prev.Value)
        {
            _logger.LogWarning("2FA replay blocked for user {UserId} (step {Step})", user.Id, matchedStep);
            return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Invalid);
        }
        await _state.SetLastReplayStepAsync(user.Id, matchedStep, ReplayGuardTtl);

        return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Valid);
    }

    // ─────────────────────────────────────────────────────────
    //  Disable / regenerate
    // ─────────────────────────────────────────────────────────

    public async Task DisableAsync(User user)
    {
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorEnabledAt = null;
        await _users.UpdateAsync(user);

        await _recoveryCodes.DeleteAllForUserAsync(user.Id);
        await _state.DeleteSetupSecretAsync(user.Id);
        await _state.DeleteReplayStepAsync(user.Id);

        _logger.LogInformation("2FA disabled for user {UserId}", user.Id);
    }

    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(User user)
    {
        if (!user.TwoFactorEnabled)
            throw new InvalidOperationException("Cannot regenerate recovery codes when 2FA is not enabled.");

        await _recoveryCodes.DeleteAllForUserAsync(user.Id);

        var (plaintextCodes, hashedEntities) = GenerateRecoveryCodes(user.Id);
        await _recoveryCodes.AddRangeAsync(hashedEntities);

        _logger.LogInformation("2FA recovery codes regenerated for user {UserId}", user.Id);
        return plaintextCodes;
    }

    public async Task<TwoFactorStatus> GetStatusAsync(User user)
    {
        var remaining = user.TwoFactorEnabled
            ? await _recoveryCodes.CountUnusedAsync(user.Id)
            : 0;

        return new TwoFactorStatus(user.TwoFactorEnabled, user.TwoFactorEnabledAt, remaining);
    }

    // ─────────────────────────────────────────────────────────
    //  Internals
    // ─────────────────────────────────────────────────────────

    private static bool VerifyTotpCode(string base32Secret, string code, out long matchedStep)
    {
        matchedStep = 0;
        if (string.IsNullOrWhiteSpace(code) || code.Length != TotpDigits || !code.All(char.IsDigit))
            return false;

        // Re-pad base32 to a multiple of 8 — Otp.NET's decoder requires it,
        // and we strip '=' when storing for nicer URIs.
        var padded = base32Secret.PadRight((base32Secret.Length + 7) / 8 * 8, '=');
        var bytes = Base32Encoding.ToBytes(padded);

        var totp = new Totp(bytes, step: TotpStepSeconds, mode: OtpHashMode.Sha1, totpSize: TotpDigits);
        // VerificationWindow(previous: 1, future: 1) -> tolerate ±1 step (clock drift).
        return totp.VerifyTotp(code, out matchedStep, new VerificationWindow(previous: 1, future: 1));
    }

    private static string BuildOtpAuthUri(string email, string base32Secret)
    {
        var label = $"{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}";
        var query =
            $"secret={base32Secret}" +
            $"&issuer={Uri.EscapeDataString(Issuer)}" +
            "&algorithm=SHA1" +
            $"&digits={TotpDigits}" +
            $"&period={TotpStepSeconds}";
        return $"otpauth://totp/{label}?{query}";
    }

    private static bool LooksLikeRecoveryCode(string s)
    {
        // 4 groups of 4 separated by '-' = 19 chars total.
        if (s.Length != RecoveryGroups * RecoveryGroupLength + (RecoveryGroups - 1)) return false;
        for (var i = 0; i < s.Length; i++)
        {
            var expectDash = (i + 1) % (RecoveryGroupLength + 1) == 0 && i < s.Length - 1;
            if (expectDash)
            {
                if (s[i] != '-') return false;
            }
            else
            {
                if (!IsHex(s[i])) return false;
            }
        }
        return true;

        static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    private async Task<bool> VerifyRecoveryCodeAsync(Guid userId, string code)
    {
        var normalized = code.ToLowerInvariant();
        var unused = await _recoveryCodes.GetUnusedAsync(userId);

        // Linear scan over <= 10 BCrypt hashes; constant cost in practice.
        foreach (var entity in unused)
        {
            if (_hasher.Verify(normalized, entity.CodeHash))
            {
                await _recoveryCodes.MarkUsedAsync(entity);
                return true;
            }
        }
        return false;
    }

    private (IReadOnlyList<string> Plaintext, IReadOnlyList<UserRecoveryCode> Hashed) GenerateRecoveryCodes(Guid userId)
    {
        var plaintext = new List<string>(RecoveryCodeCount);
        var hashed = new List<UserRecoveryCode>(RecoveryCodeCount);

        for (var i = 0; i < RecoveryCodeCount; i++)
        {
            var code = NewRecoveryCode();
            plaintext.Add(code);
            hashed.Add(new UserRecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = _hasher.Hash(code),
                CreatedAt = DateTime.UtcNow,
            });
        }

        return (plaintext, hashed);
    }

    private static string NewRecoveryCode()
    {
        // 16 hex chars, grouped 4-4-4-4. RandomNumberGenerator -> uniform draw.
        Span<char> buf = stackalloc char[RecoveryGroups * RecoveryGroupLength + (RecoveryGroups - 1)];
        var pos = 0;
        for (var g = 0; g < RecoveryGroups; g++)
        {
            if (g > 0) buf[pos++] = '-';
            for (var c = 0; c < RecoveryGroupLength; c++)
                buf[pos++] = RecoveryAlphabet[RandomNumberGenerator.GetInt32(RecoveryAlphabet.Length)];
        }
        return new string(buf);
    }
}
