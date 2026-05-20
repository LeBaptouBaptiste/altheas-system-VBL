using System.Security.Cryptography;
using OtpNet;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.Email;
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

    // Lock-out (brute-force protection) — values calibrated for a 6-digit
    // TOTP. With 5 attempts max and a 15-min lock, an attacker is capped
    // at 5 guesses per 15 min = 480 guesses/day = 0.05% chance over 24 h.
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FailureCounterTtl = TimeSpan.FromMinutes(15);

    // Recovery codes: 4 groups of 4 hex chars => 16 hex chars => 64 bits of entropy.
    private const int RecoveryGroups = 4;
    private const int RecoveryGroupLength = 4;
    private const string RecoveryAlphabet = "0123456789abcdef";

    private readonly ITwoFactorStateStore _state;
    private readonly IUserRepository _users;
    private readonly IRecoveryCodeRepository _recoveryCodes;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _hasher;
    private readonly ISecurityAlertSender _alerts;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        ITwoFactorStateStore state,
        IUserRepository users,
        IRecoveryCodeRepository recoveryCodes,
        IEncryptionService encryption,
        IPasswordHasher hasher,
        ISecurityAlertSender alerts,
        ILogger<TwoFactorService> logger)
    {
        _state = state;
        _users = users;
        _recoveryCodes = recoveryCodes;
        _encryption = encryption;
        _hasher = hasher;
        _alerts = alerts;
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

        // Phase 4a: notify the user out-of-band that 2FA was just turned on.
        // Best-effort — SMTP failure must NOT unwind the just-completed enable,
        // the user is now relying on this setting to log in.
        await FireAlertBestEffortAsync(user, SecurityAlertType.TwoFactorEnabled);

        return new TwoFactorEnableResult(plaintextCodes);
    }

    // ─────────────────────────────────────────────────────────
    //  Verify
    // ─────────────────────────────────────────────────────────

    public async Task<TwoFactorVerifyResult> VerifyAsync(User user, string codeOrRecovery)
    {
        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.NotEnabled);

        // Brute-force gate: refuse upfront if the account is currently locked.
        var lockRemaining = await _state.GetLockRemainingAsync(user.Id);
        if (lockRemaining is { } remaining && remaining > TimeSpan.Zero)
        {
            _logger.LogWarning("2FA verify blocked: user {UserId} is locked for {Seconds}s",
                user.Id, (int)remaining.TotalSeconds);
            return new TwoFactorVerifyResult(
                TwoFactorVerifyOutcome.Locked,
                RetryAfterSeconds: (int)Math.Ceiling(remaining.TotalSeconds));
        }

        var normalized = (codeOrRecovery ?? string.Empty).Trim();

        // Recovery code path: detect by length / dashes.
        if (LooksLikeRecoveryCode(normalized))
        {
            var ok = await VerifyRecoveryCodeAsync(user.Id, normalized);
            if (ok)
            {
                await _state.ResetFailuresAsync(user.Id);
                return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.ValidViaRecoveryCode);
            }
            return await RegisterFailureAsync(user.Id);
        }

        // TOTP path: 6 digits.
        var base32 = _encryption.Decrypt(user.TwoFactorSecret);
        if (!VerifyTotpCode(base32, normalized, out var matchedStep))
        {
            return await RegisterFailureAsync(user.Id);
        }

        // Replay protection: refuse if this step was already consumed.
        // Replays count as failures too — a sniffed code mustn't be free
        // tries against the brute-force counter.
        var prev = await _state.GetLastReplayStepAsync(user.Id);
        if (prev.HasValue && matchedStep <= prev.Value)
        {
            _logger.LogWarning("2FA replay blocked for user {UserId} (step {Step})", user.Id, matchedStep);
            return await RegisterFailureAsync(user.Id);
        }
        await _state.SetLastReplayStepAsync(user.Id, matchedStep, ReplayGuardTtl);

        await _state.ResetFailuresAsync(user.Id);
        return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Valid);
    }

    /// <summary>
    /// Records a failed attempt and, if the threshold is hit, sets the
    /// account-wide lock. Returns either Invalid (still has tries left)
    /// or Locked (threshold reached, with the remaining lock duration).
    /// </summary>
    private async Task<TwoFactorVerifyResult> RegisterFailureAsync(Guid userId)
    {
        var count = await _state.IncrementFailureAsync(userId, FailureCounterTtl);

        if (count >= MaxFailedAttempts)
        {
            await _state.LockAsync(userId, LockDuration);
            await _state.ResetFailuresAsync(userId);
            _logger.LogWarning(
                "2FA lock-out triggered for user {UserId} after {Count} failed attempts; locked {Minutes} min",
                userId, count, (int)LockDuration.TotalMinutes);
            return new TwoFactorVerifyResult(
                TwoFactorVerifyOutcome.Locked,
                RetryAfterSeconds: (int)LockDuration.TotalSeconds);
        }

        _logger.LogInformation(
            "2FA verify failed for user {UserId} (attempt {Count}/{Max})",
            userId, count, MaxFailedAttempts);
        return new TwoFactorVerifyResult(TwoFactorVerifyOutcome.Invalid);
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

        // Phase 4a: out-of-band alert. Especially important here — a malicious
        // step-up token misuse could disable 2FA silently otherwise.
        await FireAlertBestEffortAsync(user, SecurityAlertType.TwoFactorDisabled);
    }

    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(User user)
    {
        if (!user.TwoFactorEnabled)
            throw new InvalidOperationException("Cannot regenerate recovery codes when 2FA is not enabled.");

        await _recoveryCodes.DeleteAllForUserAsync(user.Id);

        var (plaintextCodes, hashedEntities) = GenerateRecoveryCodes(user.Id);
        await _recoveryCodes.AddRangeAsync(hashedEntities);

        _logger.LogInformation("2FA recovery codes regenerated for user {UserId}", user.Id);

        // Phase 4a: alert the user — if it wasn't them, the old codes still
        // worked moments ago and we just invalidated them all.
        await FireAlertBestEffortAsync(user, SecurityAlertType.RecoveryCodesRegenerated);

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

    /// <summary>
    /// Fires a security alert email without ever throwing. SMTP failures are
    /// logged but must NOT propagate — the underlying 2FA op already
    /// completed and committed, surfacing an email failure would force the
    /// user to retry an action that has already succeeded.
    /// </summary>
    private async Task FireAlertBestEffortAsync(User user, SecurityAlertType type)
    {
        try
        {
            await _alerts.SendAsync(user, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send {AlertType} security alert to user {UserId} ({Email}). " +
                "The underlying 2FA operation already succeeded — alert is informational only.",
                type, user.Id, user.Email);
        }
    }

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
