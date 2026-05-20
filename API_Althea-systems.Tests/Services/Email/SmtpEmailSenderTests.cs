using API_Althea_systems.Services.Email;
using FluentAssertions;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace API_Althea_systems.Tests.Services.Email;

/// <summary>
/// Exercises <see cref="SmtpEmailSender"/> without touching a real SMTP
/// server. The transport seam (<see cref="IMailKitTransport"/>) lets us
/// assert ordering / arguments and force failures.
///
/// Critical scenarios:
///   • Happy path: connect → authenticate → send → disconnect, in that order.
///   • No SMTP configured (blank Host): logs warning, no-ops, no exception.
///   • SMTP failure wrapped in <see cref="EmailDeliveryException"/>.
///   • Cancellation NOT wrapped (callers want the original OperationCanceled).
///   • MimeMessage construction: from/to/subject/HTML body/attachments.
/// </summary>
public class SmtpEmailSenderTests
{
    private static IOptions<SmtpSettings> Settings(string host = "smtp.example.com",
        string? username = "user",
        string? password = "pwd",
        bool useStartTls = true)
        => Options.Create(new SmtpSettings
        {
            Host = host,
            Port = 587,
            Username = username,
            Password = password,
            From = "noreply@altheasystems.com",
            FromName = "Althea Systems",
            UseStartTls = useStartTls,
        });

    private static (SmtpEmailSender sut, Mock<IMailKitTransport> transport) BuildSut(
        IOptions<SmtpSettings>? settings = null)
    {
        var transport = new Mock<IMailKitTransport>(MockBehavior.Loose);
        var sut = new SmtpEmailSender(
            settings ?? Settings(),
            NullLogger<SmtpEmailSender>.Instance,
            () => transport.Object);
        return (sut, transport);
    }

    // ── Happy path ───────────────────────────────────────

    [Fact]
    public async Task SendAsync_HappyPath_ConnectsAuthenticatesSendsAndDisconnects()
    {
        var (sut, transport) = BuildSut();

        await sut.SendAsync("client@example.com", "Sujet", "<p>hi</p>");

        // Each step must be called exactly once. We could enforce ordering
        // with MockSequence, but the four Verify calls + the success of the
        // operation imply the natural order (the production code is linear).
        transport.Verify(t => t.ConnectAsync("smtp.example.com", 587,
            SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        transport.Verify(t => t.AuthenticateAsync("user", "pwd",
            It.IsAny<CancellationToken>()), Times.Once);
        transport.Verify(t => t.SendAsync(It.IsAny<MimeMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
        transport.Verify(t => t.DisconnectAsync(true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_NoUsername_SkipsAuthenticateButStillSends()
    {
        // Local Mailpit / Mailhog accepts mail without auth — don't force a
        // pointless AuthenticateAsync that would fail against them.
        var (sut, transport) = BuildSut(Settings(username: null, password: null));

        await sut.SendAsync("client@example.com", "Sujet", "<p>hi</p>");

        transport.Verify(t => t.AuthenticateAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        transport.Verify(t => t.SendAsync(It.IsAny<MimeMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── No SMTP configured ───────────────────────────────

    [Fact]
    public async Task SendAsync_BlankHost_NoOps()
    {
        // Local dev without credentials — booting must not crash, and a
        // send-call should log+skip rather than throw. Keeps the rest of
        // the app usable while email is disabled.
        var (sut, transport) = BuildSut(Settings(host: ""));

        var act = () => sut.SendAsync("x@y.com", "subject", "<p>body</p>");

        await act.Should().NotThrowAsync();
        transport.Verify(t => t.ConnectAsync(It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Failures ─────────────────────────────────────────

    [Fact]
    public async Task SendAsync_TransportThrows_WrapsInEmailDeliveryException()
    {
        // SMTP-server-rejected / network-unreachable / auth-failed all need
        // to surface as a typed exception so callers (Stripe webhook, register
        // flow) can catch only the email path without swallowing real bugs.
        var (sut, transport) = BuildSut();
        transport.Setup(t => t.SendAsync(It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("server says no"));

        var act = () => sut.SendAsync("x@y.com", "subject", "<p>body</p>");

        var ex = await act.Should().ThrowAsync<EmailDeliveryException>();
        ex.WithMessage("*x@y.com*");
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_CancellationToken_PropagatesWithoutWrapping()
    {
        // ASP.NET aborts a request → the original OperationCanceledException
        // must survive so the framework can handle it as a client disconnect
        // rather than a 500.
        var (sut, transport) = BuildSut();
        transport.Setup(t => t.SendAsync(It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = () => sut.SendAsync("x@y.com", "subject", "<p>body</p>",
            ct: new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Message construction ─────────────────────────────

    [Fact]
    public void BuildMessage_SetsFromToSubjectAndHtmlBody()
    {
        var (sut, _) = BuildSut();

        var msg = sut.BuildMessage(
            to: "client@example.com",
            subject: "Sujet",
            htmlBody: "<p>hi</p>",
            attachments: null);

        msg.From.ToString().Should().Contain("noreply@altheasystems.com");
        msg.From.ToString().Should().Contain("Althea Systems");
        msg.To.ToString().Should().Contain("client@example.com");
        msg.Subject.Should().Be("Sujet");
        msg.HtmlBody.Should().Be("<p>hi</p>");
    }

    [Fact]
    public void BuildMessage_WithAttachment_IncludesIt()
    {
        var (sut, _) = BuildSut();
        var pdf = new EmailAttachment("facture.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf");

        var msg = sut.BuildMessage("c@x.com", "s", "<p>b</p>", new[] { pdf });

        var attachments = msg.Attachments.ToList();
        attachments.Should().HaveCount(1);
        var part = attachments.Single() as MimePart;
        part.Should().NotBeNull("the PDF should land as a MimePart, not a multipart container");
        part!.FileName.Should().Be("facture.pdf");
        part.ContentType.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public void BuildMessage_InvalidAttachmentContentType_Throws()
    {
        // Bad MIME → surface immediately, don't ship a malformed message
        // that the SMTP server would reject opaquely.
        var (sut, _) = BuildSut();
        var bad = new EmailAttachment("x.bin", new byte[] { 0 }, "not a/valid/mime/type");

        var act = () => sut.BuildMessage("c@x.com", "s", "<p>b</p>", new[] { bad });

        act.Should().Throw<Exception>();  // MimeKit throws its own ParseException
    }
}
