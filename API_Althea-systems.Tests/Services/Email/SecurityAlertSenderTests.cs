using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API_Althea_systems.Tests.Services.Email;

/// <summary>
/// Verifies the sender picks the right subject + template placeholders for
/// each alert type. The template renderer is a real instance built from an
/// in-memory dictionary so we exercise the actual placeholder substitution.
/// </summary>
public class SecurityAlertSenderTests
{
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly IEmailTemplateRenderer _renderer;
    private readonly SecurityAlertSender _sut;

    // Minimal template — just enough to prove the placeholders go in.
    // Real Templates/Emails/security-alert.html is exercised by the
    // integration boot (template loaded from disk).
    private const string AlertTemplate =
        "<h1>{{title}}</h1><p>Hi {{firstName}},</p><p>{{message}}</p><p>{{when}}</p>";

    public SecurityAlertSenderTests()
    {
        _renderer = new EmailTemplateRenderer(new Dictionary<string, string>
        {
            ["security-alert"] = AlertTemplate,
        });
        _sut = new SecurityAlertSender(
            _emailSender.Object,
            _renderer,
            NullLogger<SecurityAlertSender>.Instance);
    }

    private static User MakeUser(string name = "Baptiste Voyager") => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@test.com",
        Name = name,
        PasswordHash = "x",
    };

    [Fact]
    public async Task SendAsync_TwoFactorEnabled_SubjectAndBodyMatch()
    {
        var user = MakeUser();

        await _sut.SendAsync(user, SecurityAlertType.TwoFactorEnabled);

        _emailSender.Verify(s => s.SendAsync(
            user.Email,
            It.Is<string>(subj => subj.Contains("activée", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body =>
                body.Contains("Authentification à deux facteurs activée")
                && body.Contains("Baptiste")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_TwoFactorDisabled_SubjectAndBodyMatch()
    {
        var user = MakeUser();

        await _sut.SendAsync(user, SecurityAlertType.TwoFactorDisabled);

        _emailSender.Verify(s => s.SendAsync(
            user.Email,
            It.Is<string>(subj => subj.Contains("désactivée", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body => body.Contains("désactivée")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_RecoveryCodesRegenerated_SubjectAndBodyMatch()
    {
        var user = MakeUser();

        await _sut.SendAsync(user, SecurityAlertType.RecoveryCodesRegenerated);

        _emailSender.Verify(s => s.SendAsync(
            user.Email,
            It.Is<string>(subj => subj.Contains("récupération", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body => body.Contains("nouvelle série")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_FirstNameFallsBackToEmailLocalPart_WhenNameIsBlank()
    {
        // Edge: anonymised / pre-seed users may have a blank Name. The local
        // part of the email is a reasonable greeting fallback ("Hi user,").
        var user = MakeUser(name: "");

        await _sut.SendAsync(user, SecurityAlertType.TwoFactorEnabled);

        _emailSender.Verify(s => s.SendAsync(
            user.Email,
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("Hi user,")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_FirstNameUsesFirstWord_WhenFullNameProvided()
    {
        // "Baptiste Voyager" → greeting should be "Baptiste", not the whole name.
        var user = MakeUser(name: "Baptiste Voyager");

        await _sut.SendAsync(user, SecurityAlertType.TwoFactorEnabled);

        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(body =>
                body.Contains("Hi Baptiste,")
                && !body.Contains("Hi Baptiste Voyager,")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_UnknownAlertType_Throws()
    {
        // Defensive — passing an out-of-range enum value must fail loud so a
        // future add doesn't silently send an alert with empty subject/body.
        var user = MakeUser();

        var act = () => _sut.SendAsync(user, (SecurityAlertType)999);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SendAsync_NullUser_Throws()
    {
        var act = () => _sut.SendAsync(null!, SecurityAlertType.TwoFactorEnabled);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
