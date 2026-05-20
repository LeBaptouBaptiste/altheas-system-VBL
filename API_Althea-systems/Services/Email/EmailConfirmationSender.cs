using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Bound to "EmailConfirmation" — settings that govern how the welcome /
/// confirmation email is built, but NOT the SMTP transport (that's
/// <see cref="SmtpSettings"/>).
/// </summary>
public class EmailConfirmationOptions
{
    /// <summary>
    /// Public origin of the frontend, e.g. "https://altheasystems.com" in prod
    /// or "http://localhost:3000" in dev. The confirmation link is built as
    /// <c>{FrontendBaseUrl}/confirm-email?token=…</c>.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>How long a freshly issued token stays valid. Defaults to 24 h.</summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Convenience layer over <see cref="IEmailSender"/> + <see cref="IEmailTemplateRenderer"/>
/// for the registration-confirmation flow. Lives here (not in AuthService) so
/// AuthService stays focused on auth logic and the email machinery is reused
/// by the resend endpoint without duplication.
/// </summary>
public interface IEmailConfirmationSender
{
    /// <summary>
    /// Renders the "welcome" template with the user's name + the link built
    /// around <paramref name="rawToken"/>, then sends it. Surfaces
    /// <see cref="EmailDeliveryException"/> on failure so the caller decides
    /// whether to roll back the registration or log + continue.
    /// </summary>
    Task SendAsync(User user, string rawToken, CancellationToken ct = default);
}

public class EmailConfirmationSender : IEmailConfirmationSender
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly EmailConfirmationOptions _options;

    public EmailConfirmationSender(
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer,
        IOptions<EmailConfirmationOptions> options)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _options = options.Value;
    }

    public async Task SendAsync(User user, string rawToken, CancellationToken ct = default)
    {
        // Trim trailing slash so we don't end up with "…//confirm-email".
        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(rawToken)}";

        var html = _renderer.Render("welcome", new Dictionary<string, string>
        {
            // Display name — falls back to the email local-part if Name is
            // blank (shouldn't happen given the RegisterRequest validator,
            // but cheap defence).
            ["firstName"] = string.IsNullOrWhiteSpace(user.Name)
                ? user.Email.Split('@')[0]
                : user.Name,
            ["confirmLink"] = link,
            // Hours, not seconds — the template phrasing is "within 24 h".
            ["tokenLifetimeHours"] = ((int)_options.TokenLifetime.TotalHours).ToString(),
        });

        await _emailSender.SendAsync(
            to: user.Email,
            subject: "Confirmez votre adresse email — Althea Systems",
            htmlBody: html,
            ct: ct);
    }
}
