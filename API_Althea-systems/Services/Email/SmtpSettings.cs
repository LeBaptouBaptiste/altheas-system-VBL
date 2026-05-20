namespace API_Althea_systems.Services.Email;

/// <summary>
/// Bound to the "Smtp" section of appsettings.json / env vars (Smtp__*).
/// Works with any SMTP provider: Mailtrap / Brevo / OVH / Gmail / SendGrid SMTP.
/// </summary>
public class SmtpSettings
{
    /// <summary>SMTP hostname, e.g. "live.smtp.mailtrap.io".</summary>
    public string Host { get; set; } = "";

    /// <summary>
    /// SMTP port. Common values: 587 (StartTLS), 465 (SSL/TLS implicit),
    /// 2525 (fallback for Mailtrap when 587 is blocked).
    /// </summary>
    public int Port { get; set; } = 587;

    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>From address used as the envelope sender, e.g. "noreply@altheasystems.com".</summary>
    public string From { get; set; } = "noreply@altheasystems.com";

    /// <summary>Display name for the From header, e.g. "Althea Systems".</summary>
    public string FromName { get; set; } = "Althea Systems";

    /// <summary>
    /// True for port 587-style "upgrade to TLS after EHLO". False uses MailKit's
    /// Auto detection (covers implicit TLS on 465 and plain on 25 for local Mailpit
    /// / Mailhog).
    /// </summary>
    public bool UseStartTls { get; set; } = true;
}
