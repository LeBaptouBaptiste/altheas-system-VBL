using API_Althea_systems.Services.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// MailKit-based <see cref="IEmailSender"/>. The transport is constructed
/// per-call (cheap, MailKit is designed for this) so we don't have to deal
/// with idle-disconnect / re-auth state between sends.
///
/// If <see cref="SmtpSettings.Host"/> is blank (e.g. local dev without SMTP
/// credentials), every send is a logged no-op — the API keeps booting and
/// other flows continue to work without email.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;
    // Test seam: lets unit tests inject a fake transport without touching
    // a real SMTP server. The default factory returns a MailKit SmtpClient.
    private readonly Func<IMailKitTransport> _transportFactory;

    /// <summary>
    /// Single constructor (DI-friendly — no ambiguity for ActivatorUtilities).
    /// Production callers and DI omit the factory and get a real MailKit client;
    /// unit tests pass a mock transport.
    /// </summary>
    public SmtpEmailSender(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailSender> logger,
        Func<IMailKitTransport>? transportFactory = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _transportFactory = transportFactory
            ?? (() => new MailKitTransportAdapter(new SmtpClient()));
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            // No SMTP configured — log and skip. Tests / local dev without
            // Mailtrap credentials hit this path; behavior matches "best-effort"
            // semantics callers expect (see XML doc on IEmailSender).
            _logger.LogWarning(
                "SMTP not configured (Smtp:Host is blank). Skipping email to {To} subject={Subject}.",
                to, subject);
            return;
        }

        var message = BuildMessage(to, subject, htmlBody, attachments);

        try
        {
            using var transport = _transportFactory();
            var socketOptions = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await transport.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await transport.AuthenticateAsync(_settings.Username, _settings.Password ?? "", ct);
            }

            await transport.SendAsync(message, ct);
            await transport.DisconnectAsync(quit: true, ct);

            _logger.LogInformation(
                "Email sent to {To} subject=\"{Subject}\" attachments={AttachmentCount}",
                to, subject, attachments?.Count() ?? 0);
        }
        catch (OperationCanceledException)
        {
            // Don't wrap cancellation — callers (e.g. ASP.NET request abort)
            // want to see the original token cancellation, not a delivery error.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {To} subject=\"{Subject}\"",
                to, subject);
            throw new EmailDeliveryException(
                $"Failed to send email to {to}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Builds the <see cref="MimeMessage"/> without touching the network.
    /// Exposed so tests can assert on header / body / attachment construction
    /// without a fake SMTP server.
    /// </summary>
    public MimeMessage BuildMessage(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };

        if (attachments is not null)
        {
            foreach (var att in attachments)
            {
                // ContentType.Parse throws on invalid MIME — propagate as-is
                // so a bad caller sees a precise error instead of an opaque
                // SMTP failure later.
                builder.Attachments.Add(att.FileName, att.Content, ContentType.Parse(att.ContentType));
            }
        }

        message.Body = builder.ToMessageBody();
        return message;
    }
}

/// <summary>
/// Minimal abstraction over MailKit's <see cref="SmtpClient"/> so we can mock
/// it in unit tests without dragging in MailKit's heavier <c>IMailTransport</c>
/// surface. Keeps only the four methods this sender actually uses.
/// </summary>
public interface IMailKitTransport : IDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken ct);
    Task AuthenticateAsync(string username, string password, CancellationToken ct);
    Task SendAsync(MimeMessage message, CancellationToken ct);
    Task DisconnectAsync(bool quit, CancellationToken ct);
}

internal sealed class MailKitTransportAdapter : IMailKitTransport
{
    private readonly SmtpClient _client;
    public MailKitTransportAdapter(SmtpClient client) { _client = client; }

    public Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken ct)
        => _client.ConnectAsync(host, port, options, ct);

    public Task AuthenticateAsync(string username, string password, CancellationToken ct)
        => _client.AuthenticateAsync(username, password, ct);

    public Task SendAsync(MimeMessage message, CancellationToken ct)
        => _client.SendAsync(message, ct);

    public Task DisconnectAsync(bool quit, CancellationToken ct)
        => _client.DisconnectAsync(quit, ct);

    public void Dispose() => _client.Dispose();
}
