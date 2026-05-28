using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OtpNet;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Services;

public class TwoFactorServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly FakeRecoveryCodeRepo _recoveryRepo = new();
    private readonly FakeStateStore _state = new();
    private readonly EncryptionService _encryption;
    private readonly TestPasswordHasher _hasher = new();
    private readonly Mock<ISecurityAlertSender> _alerts = new();
    private readonly Mock<ITwoFactorCodeSender> _codeSender = new();
    private readonly TwoFactorService _sut;

    public TwoFactorServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            })
            .Build();
        _encryption = new EncryptionService(config);

        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _sut = new TwoFactorService(
            _state,
            _userRepo.Object,
            _recoveryRepo,
            _encryption,
            _hasher,
            _alerts.Object,
            _codeSender.Object,
            NullLogger<TwoFactorService>.Instance);
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@test.com",
        Name = "Test User",
        PasswordHash = "irrelevant",
    };

    private static string ComputeTotp(string base32Secret)
    {
        var padded = base32Secret.PadRight((base32Secret.Length + 7) / 8 * 8, '=');
        var totp = new Totp(Base32Encoding.ToBytes(padded));
        return totp.ComputeTotp();
    }

    // ─────────────────────────────────────────────────────────
    //  StartSetup
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StartSetupAsync_StoresPendingSecretInStateStore()
    {
        var user = NewUser();

        var result = await _sut.StartSetupAsync(user);

        result.Secret.Should().NotBeNullOrEmpty();
        _state.SetupSecrets.Should().ContainKey(user.Id);
        _state.SetupSecrets[user.Id].Should().Be(result.Secret);
    }

    [Fact]
    public async Task StartSetupAsync_ProducesValidOtpAuthUri()
    {
        var user = NewUser();

        var result = await _sut.StartSetupAsync(user);

        result.OtpAuthUri.Should().StartWith("otpauth://totp/AltheaSystems:");
        result.OtpAuthUri.Should().Contain("secret=" + result.Secret);
        result.OtpAuthUri.Should().Contain("issuer=AltheaSystems");
        result.OtpAuthUri.Should().Contain("digits=6");
        result.OtpAuthUri.Should().Contain("period=30");
    }

    // ─────────────────────────────────────────────────────────
    //  Enable
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EnableAsync_ValidCode_ActivatesAndReturns10RecoveryCodes()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        var code = ComputeTotp(setup.Secret);

        var result = await _sut.EnableAsync(user, code);

        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorEnabledAt.Should().NotBeNull();
        user.TwoFactorSecret.Should().NotBeNullOrEmpty().And.NotBe(setup.Secret); // encrypted
        result.RecoveryCodes.Should().HaveCount(10);
        _recoveryRepo.Codes.Should().HaveCount(10);
        _state.SetupSecrets.Should().NotContainKey(user.Id); // pending wiped
    }

    [Fact]
    public async Task EnableAsync_RecoveryCodesMatchExpectedFormat()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);

        var result = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        result.RecoveryCodes.Should().AllSatisfy(c =>
        {
            // 4-4-4-4 hex
            System.Text.RegularExpressions.Regex.IsMatch(c, "^[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}$")
                .Should().BeTrue($"recovery code '{c}' should match 4-4-4-4 hex format");
        });
        result.RecoveryCodes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task EnableAsync_NoPendingSetup_Throws()
    {
        var user = NewUser();

        var act = () => _sut.EnableAsync(user, "123456");

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*No pending 2FA setup*");
    }

    [Fact]
    public async Task EnableAsync_WrongCode_ThrowsAndDoesNotMutateUser()
    {
        var user = NewUser();
        await _sut.StartSetupAsync(user);

        var act = () => _sut.EnableAsync(user, "000000");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid TOTP*");
        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────
    //  Verify
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_TwoFactorOff_ReturnsNotEnabled()
    {
        var user = NewUser();

        var result = await _sut.VerifyAsync(user, "123456");

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.NotEnabled);
    }

    [Fact]
    public async Task VerifyAsync_InvalidTotp_ReturnsInvalid()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        var result = await _sut.VerifyAsync(user, "000000");

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid);
    }

    [Fact]
    public async Task VerifyAsync_SameTotpTwice_SecondAttemptIsBlockedAsReplay()
    {
        // Activate without going through Enable (which itself consumes a step
        // implicitly — we want to control the replay-guard interactions).
        var user = NewUser();
        var rawSecret = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20)).TrimEnd('=');
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = TwoFactorMethod.Authenticator;
        user.TwoFactorEnabledAt = DateTime.UtcNow;
        user.TwoFactorSecret = _encryption.Encrypt(rawSecret);

        var code = ComputeTotp(rawSecret);

        var first = await _sut.VerifyAsync(user, code);
        var second = await _sut.VerifyAsync(user, code);

        first.Outcome.Should().Be(TwoFactorVerifyOutcome.Valid);
        second.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid);
    }

    [Fact]
    public async Task VerifyAsync_ValidRecoveryCode_ReturnsValidViaRecoveryCode_AndConsumesIt()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        var enable = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));
        var aCode = enable.RecoveryCodes[0];

        var first = await _sut.VerifyAsync(user, aCode);
        var second = await _sut.VerifyAsync(user, aCode);

        first.Outcome.Should().Be(TwoFactorVerifyOutcome.ValidViaRecoveryCode);
        second.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid); // single-use
        _recoveryRepo.Codes.Count(c => c.UsedAt != null).Should().Be(1);
    }

    [Fact]
    public async Task VerifyAsync_WellFormedButUnknownRecoveryCode_ReturnsInvalid()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        var result = await _sut.VerifyAsync(user, "deaf-beef-cafe-1234");

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid);
    }

    // ─────────────────────────────────────────────────────────
    //  Lock-out (brute-force protection)
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_AccumulatesFailures_LocksAfterFiveAttempts()
    {
        var user = NewUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = TwoFactorMethod.Authenticator;
        user.TwoFactorEnabledAt = DateTime.UtcNow;
        user.TwoFactorSecret = _encryption.Encrypt(
            Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20)).TrimEnd('='));

        // 4 first wrong attempts come back as Invalid; the 5th flips to Locked.
        for (var i = 1; i <= 4; i++)
        {
            var r = await _sut.VerifyAsync(user, "000000");
            r.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid, $"attempt {i}");
        }

        var fifth = await _sut.VerifyAsync(user, "000000");
        fifth.Outcome.Should().Be(TwoFactorVerifyOutcome.Locked);
        fifth.RetryAfterSeconds.Should().BeGreaterThan(0);
        _state.Locks.Should().ContainKey(user.Id);
    }

    [Fact]
    public async Task VerifyAsync_LockedAccount_RefusesEvenValidCodes()
    {
        var user = NewUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = TwoFactorMethod.Authenticator;
        var raw = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20)).TrimEnd('=');
        user.TwoFactorSecret = _encryption.Encrypt(raw);

        // Pre-lock the account directly.
        await _state.LockAsync(user.Id, TimeSpan.FromMinutes(15));

        // Even a valid TOTP must be rejected while locked.
        var validCode = ComputeTotp(raw);
        var result = await _sut.VerifyAsync(user, validCode);

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.Locked);
        result.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task VerifyAsync_SuccessfulCode_ResetsFailureCounter()
    {
        var user = NewUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorMethod = TwoFactorMethod.Authenticator;
        var raw = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20)).TrimEnd('=');
        user.TwoFactorSecret = _encryption.Encrypt(raw);

        // Build up a few failures.
        await _sut.VerifyAsync(user, "000000");
        await _sut.VerifyAsync(user, "000000");
        _state.FailureCounts[user.Id].Should().Be(2);

        // A success wipes the counter.
        var ok = await _sut.VerifyAsync(user, ComputeTotp(raw));
        ok.Outcome.Should().Be(TwoFactorVerifyOutcome.Valid);
        _state.FailureCounts.Should().NotContainKey(user.Id);
    }

    [Fact]
    public async Task VerifyAsync_RecoveryCodeSuccess_ResetsFailureCounter()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        var enable = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        // Fail twice with bad codes.
        await _sut.VerifyAsync(user, "000000");
        await _sut.VerifyAsync(user, "111111");
        _state.FailureCounts[user.Id].Should().Be(2);

        // Use a recovery code -> success -> counter reset.
        var ok = await _sut.VerifyAsync(user, enable.RecoveryCodes[0]);
        ok.Outcome.Should().Be(TwoFactorVerifyOutcome.ValidViaRecoveryCode);
        _state.FailureCounts.Should().NotContainKey(user.Id);
    }

    // ─────────────────────────────────────────────────────────
    //  Disable / regenerate / status
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisableAsync_ClearsSecretRecoveryCodesAndState()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        await _sut.DisableAsync(user);

        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
        user.TwoFactorEnabledAt.Should().BeNull();
        _recoveryRepo.Codes.Where(c => c.UserId == user.Id).Should().BeEmpty();
        _state.SetupSecrets.Should().NotContainKey(user.Id);
        _state.ReplaySteps.Should().NotContainKey(user.Id);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_ReplacesAllCodes()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        var first = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        var second = await _sut.RegenerateRecoveryCodesAsync(user);

        second.Should().HaveCount(10);
        second.Should().NotIntersectWith(first.RecoveryCodes);
        _recoveryRepo.Codes.Should().HaveCount(10); // old ones deleted
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_TwoFactorOff_Throws()
    {
        var user = NewUser();

        var act = () => _sut.RegenerateRecoveryCodesAsync(user);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetStatusAsync_ReflectsEnabledStateAndRemainingCount()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        var enable = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        var beforeUse = await _sut.GetStatusAsync(user);
        await _sut.VerifyAsync(user, enable.RecoveryCodes[0]);
        var afterUse = await _sut.GetStatusAsync(user);

        beforeUse.Enabled.Should().BeTrue();
        beforeUse.RecoveryCodesRemaining.Should().Be(10);
        afterUse.RecoveryCodesRemaining.Should().Be(9);
    }

    // ═════════════════════════════════════════════════════════
    //  Phase 4a — security alert side-effects
    // ═════════════════════════════════════════════════════════

    [Fact]
    public async Task EnableAsync_FiresTwoFactorEnabledAlert()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);

        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        _alerts.Verify(s => s.SendAsync(user, SecurityAlertType.TwoFactorEnabled,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableAsync_FiresTwoFactorDisabledAlert()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));
        _alerts.Invocations.Clear();

        await _sut.DisableAsync(user);

        _alerts.Verify(s => s.SendAsync(user, SecurityAlertType.TwoFactorDisabled,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_FiresRegeneratedAlert()
    {
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));
        _alerts.Invocations.Clear();

        await _sut.RegenerateRecoveryCodesAsync(user);

        _alerts.Verify(s => s.SendAsync(user, SecurityAlertType.RecoveryCodesRegenerated,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnableAsync_AlertSendFails_DoesNotRollBackEnable()
    {
        // Best-effort contract: a flaky SMTP must NOT undo the just-completed
        // enable. The user is now relying on 2FA being on; an exception here
        // would force them to retry a setup that has already succeeded.
        _alerts
            .Setup(s => s.SendAsync(It.IsAny<User>(), It.IsAny<SecurityAlertType>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);

        var result = await _sut.EnableAsync(user, ComputeTotp(setup.Secret));

        // Operation succeeded — recovery codes generated, user flagged enabled.
        result.RecoveryCodes.Should().HaveCount(10);
        user.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisableAsync_AlertSendFails_StillDisables()
    {
        // Same defensive contract — flipping TwoFactorEnabled=false has
        // already been committed; surfacing the SMTP error would lie about
        // the actual state.
        var user = NewUser();
        var setup = await _sut.StartSetupAsync(user);
        await _sut.EnableAsync(user, ComputeTotp(setup.Secret));
        _alerts
            .Setup(s => s.SendAsync(It.IsAny<User>(), SecurityAlertType.TwoFactorDisabled,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));

        var act = () => _sut.DisableAsync(user);

        await act.Should().NotThrowAsync();
        user.TwoFactorEnabled.Should().BeFalse();
    }

    // ═════════════════════════════════════════════════════════
    //  Phase 4b — email-based 2FA
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Captures the code emitted by StartEmailSetupAsync so the test can
    /// type it back into EnableEmailAsync (the service generates random
    /// digits — we don't fake the RNG, we just intercept the sender call).
    /// </summary>
    private void CaptureSentCode(Action<string> onSent)
    {
        _codeSender
            .Setup(s => s.SendAsync(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<TwoFactorCodePurpose>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback<User, string, TwoFactorCodePurpose, TimeSpan, CancellationToken>(
                (_, code, _, _, _) => onSent(code))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task StartEmailSetupAsync_StoresHashAndSendsSetupCode()
    {
        var user = NewUser();
        string? sentCode = null;
        CaptureSentCode(c => sentCode = c);

        await _sut.StartEmailSetupAsync(user);

        sentCode.Should().NotBeNullOrEmpty();
        sentCode!.Length.Should().Be(6);
        sentCode.Should().MatchRegex("^[0-9]{6}$");
        // Hash stored — service exposes it via the fake state store.
        _state.EmailCodeHashes.Should().ContainKey(user.Id);
        _codeSender.Verify(s => s.SendAsync(
            user, sentCode, TwoFactorCodePurpose.Setup, It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnableEmailAsync_ValidCode_ActivatesWithMethodEmail()
    {
        var user = NewUser();
        string? sentCode = null;
        CaptureSentCode(c => sentCode = c);
        await _sut.StartEmailSetupAsync(user);

        var result = await _sut.EnableEmailAsync(user, sentCode!);

        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorMethod.Should().Be(TwoFactorMethod.Email);
        user.TwoFactorEnabledAt.Should().NotBeNull();
        result.RecoveryCodes.Should().HaveCount(10);
        // Pending code wiped after success.
        _state.EmailCodeHashes.Should().NotContainKey(user.Id);
        // Phase 4a alert still fires for Email enable.
        _alerts.Verify(s => s.SendAsync(user, SecurityAlertType.TwoFactorEnabled,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnableEmailAsync_InvalidCode_Throws_AndDoesNotEnable()
    {
        var user = NewUser();
        CaptureSentCode(_ => { });
        await _sut.StartEmailSetupAsync(user);

        var act = () => _sut.EnableEmailAsync(user, "000000");

        await act.Should().ThrowAsync<InvalidOperationException>();
        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorMethod.Should().Be(TwoFactorMethod.None);
    }

    [Fact]
    public async Task EnableEmailAsync_NoPendingSetup_Throws()
    {
        var user = NewUser();

        var act = () => _sut.EnableEmailAsync(user, "123456");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RequestLoginEmailCodeAsync_OnlyFiresForEmailMethod()
    {
        // Authenticator user → no-op. Defensive: prevents leaking a Redis
        // key + sending a useless mail if some caller forgets to gate.
        var authUser = NewUser();
        var setup = await _sut.StartSetupAsync(authUser);
        await _sut.EnableAsync(authUser, ComputeTotp(setup.Secret));
        _codeSender.Invocations.Clear();

        await _sut.RequestLoginEmailCodeAsync(authUser);

        _codeSender.Verify(s => s.SendAsync(It.IsAny<User>(), It.IsAny<string>(),
            It.IsAny<TwoFactorCodePurpose>(), It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestLoginEmailCodeAsync_EmailUser_SendsLoginCode()
    {
        var user = NewUser();
        string? sentSetupCode = null;
        CaptureSentCode(c => sentSetupCode = c);
        await _sut.StartEmailSetupAsync(user);
        await _sut.EnableEmailAsync(user, sentSetupCode!);
        _codeSender.Invocations.Clear();

        string? loginCode = null;
        CaptureSentCode(c => loginCode = c);

        await _sut.RequestLoginEmailCodeAsync(user);

        loginCode.Should().NotBeNullOrEmpty();
        _codeSender.Verify(s => s.SendAsync(user, loginCode!,
            TwoFactorCodePurpose.Login, It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyAsync_EmailMethod_ValidCode_Succeeds_AndIsSingleUse()
    {
        var user = NewUser();
        string? setupCode = null;
        CaptureSentCode(c => setupCode = c);
        await _sut.StartEmailSetupAsync(user);
        await _sut.EnableEmailAsync(user, setupCode!);

        string? loginCode = null;
        CaptureSentCode(c => loginCode = c);
        await _sut.RequestLoginEmailCodeAsync(user);

        var first = await _sut.VerifyAsync(user, loginCode!);
        var second = await _sut.VerifyAsync(user, loginCode!);

        first.Outcome.Should().Be(TwoFactorVerifyOutcome.Valid);
        // Single-use — Redis entry deleted on first success.
        second.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid);
    }

    [Fact]
    public async Task VerifyAsync_EmailMethod_WrongCode_CountsAsFailure()
    {
        var user = NewUser();
        string? setupCode = null;
        CaptureSentCode(c => setupCode = c);
        await _sut.StartEmailSetupAsync(user);
        await _sut.EnableEmailAsync(user, setupCode!);
        CaptureSentCode(_ => { });
        await _sut.RequestLoginEmailCodeAsync(user);

        var result = await _sut.VerifyAsync(user, "000000");

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.Invalid);
        // Failure counter incremented — same lock-out budget applies.
        _state.FailureCounts.Should().ContainKey(user.Id);
    }

    [Fact]
    public async Task VerifyAsync_EmailMethod_RecoveryCodeStillWorks()
    {
        // Recovery codes are method-independent — they're the fallback
        // when the user lost access to their primary channel.
        var user = NewUser();
        string? setupCode = null;
        CaptureSentCode(c => setupCode = c);
        await _sut.StartEmailSetupAsync(user);
        var enable = await _sut.EnableEmailAsync(user, setupCode!);

        var result = await _sut.VerifyAsync(user, enable.RecoveryCodes[0]);

        result.Outcome.Should().Be(TwoFactorVerifyOutcome.ValidViaRecoveryCode);
    }

    [Fact]
    public async Task DisableAsync_EmailMethod_ResetsMethodAndClearsCode()
    {
        var user = NewUser();
        string? setupCode = null;
        CaptureSentCode(c => setupCode = c);
        await _sut.StartEmailSetupAsync(user);
        await _sut.EnableEmailAsync(user, setupCode!);
        CaptureSentCode(_ => { });
        await _sut.RequestLoginEmailCodeAsync(user);

        await _sut.DisableAsync(user);

        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorMethod.Should().Be(TwoFactorMethod.None);
        _state.EmailCodeHashes.Should().NotContainKey(user.Id,
            "a malicious actor with a stale Redis read must not retain a usable code post-disable");
    }

    [Fact]
    public async Task GetStatusAsync_IncludesMethodField()
    {
        var user = NewUser();
        string? setupCode = null;
        CaptureSentCode(c => setupCode = c);
        await _sut.StartEmailSetupAsync(user);
        await _sut.EnableEmailAsync(user, setupCode!);

        var status = await _sut.GetStatusAsync(user);

        status.Enabled.Should().BeTrue();
        status.Method.Should().Be(TwoFactorMethod.Email);
        status.RecoveryCodesRemaining.Should().Be(10);
    }

    // ═════════════════════════════════════════════════════════
    //  Test doubles
    // ═════════════════════════════════════════════════════════

    /// <summary>In-memory state store — replaces the Redis-backed one in tests.</summary>
    private class FakeStateStore : ITwoFactorStateStore
    {
        public Dictionary<Guid, string> SetupSecrets { get; } = new();
        public Dictionary<Guid, long> ReplaySteps { get; } = new();
        public Dictionary<Guid, int> FailureCounts { get; } = new();
        public Dictionary<Guid, DateTime> Locks { get; } = new();

        public Task SetSetupSecretAsync(Guid userId, string secret, TimeSpan ttl)
        {
            SetupSecrets[userId] = secret;
            return Task.CompletedTask;
        }

        public Task<string?> GetSetupSecretAsync(Guid userId)
            => Task.FromResult(SetupSecrets.TryGetValue(userId, out var v) ? v : null);

        public Task DeleteSetupSecretAsync(Guid userId)
        {
            SetupSecrets.Remove(userId);
            return Task.CompletedTask;
        }

        public Task<long?> GetLastReplayStepAsync(Guid userId)
            => Task.FromResult(ReplaySteps.TryGetValue(userId, out var v) ? v : (long?)null);

        public Task SetLastReplayStepAsync(Guid userId, long step, TimeSpan ttl)
        {
            ReplaySteps[userId] = step;
            return Task.CompletedTask;
        }

        public Task DeleteReplayStepAsync(Guid userId)
        {
            ReplaySteps.Remove(userId);
            return Task.CompletedTask;
        }

        public Task<int> IncrementFailureAsync(Guid userId, TimeSpan ttl)
        {
            FailureCounts.TryGetValue(userId, out var current);
            var next = current + 1;
            FailureCounts[userId] = next;
            return Task.FromResult(next);
        }

        public Task ResetFailuresAsync(Guid userId)
        {
            FailureCounts.Remove(userId);
            return Task.CompletedTask;
        }

        public Task LockAsync(Guid userId, TimeSpan ttl)
        {
            Locks[userId] = DateTime.UtcNow.Add(ttl);
            return Task.CompletedTask;
        }

        public Task<TimeSpan?> GetLockRemainingAsync(Guid userId)
        {
            if (!Locks.TryGetValue(userId, out var until)) return Task.FromResult<TimeSpan?>(null);
            var remaining = until - DateTime.UtcNow;
            return Task.FromResult<TimeSpan?>(remaining > TimeSpan.Zero ? remaining : null);
        }

        // Phase 4b — email codes
        public Dictionary<Guid, string> EmailCodeHashes { get; } = new();

        public Task SetEmailCodeHashAsync(Guid userId, string codeHash, TimeSpan ttl)
        {
            EmailCodeHashes[userId] = codeHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailCodeHashAsync(Guid userId)
            => Task.FromResult(EmailCodeHashes.TryGetValue(userId, out var v) ? v : null);

        public Task DeleteEmailCodeAsync(Guid userId)
        {
            EmailCodeHashes.Remove(userId);
            return Task.CompletedTask;
        }
    }

    private class FakeRecoveryCodeRepo : IRecoveryCodeRepository
    {
        public List<UserRecoveryCode> Codes { get; } = new();

        public Task<IReadOnlyList<UserRecoveryCode>> GetUnusedAsync(Guid userId)
            => Task.FromResult<IReadOnlyList<UserRecoveryCode>>(
                Codes.Where(c => c.UserId == userId && c.UsedAt == null).ToList());

        public Task<int> CountUnusedAsync(Guid userId)
            => Task.FromResult(Codes.Count(c => c.UserId == userId && c.UsedAt == null));

        public Task AddRangeAsync(IEnumerable<UserRecoveryCode> codes)
        {
            Codes.AddRange(codes);
            return Task.CompletedTask;
        }

        public Task MarkUsedAsync(UserRecoveryCode code)
        {
            var c = Codes.FirstOrDefault(x => x.Id == code.Id);
            if (c != null) c.UsedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteAllForUserAsync(Guid userId)
        {
            Codes.RemoveAll(c => c.UserId == userId);
            return Task.CompletedTask;
        }
    }

    /// <summary>Trivial password hasher so tests don't pay BCrypt's cost factor.</summary>
    private class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "h:" + password;
        public bool Verify(string password, string hash) => hash == "h:" + password;
        public bool MeetsRequirements(string password, out IList<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
