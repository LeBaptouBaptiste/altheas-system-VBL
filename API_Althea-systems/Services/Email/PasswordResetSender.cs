using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 5: bound to "PasswordReset" — controls how long a reset link is
/// usable and which frontend origin the link points at. Independent from
/// <see cref="EmailConfirmationOptions"/> so the two TTLs can drift.
/// </summary>
public class PasswordResetOptions
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// How long a reset token stays valid. 30 min by default — short
    /// because a leaked link is full account takeover.
    /// </summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(30);
}

public interface IPasswordResetSender
{
    /// <summary>
    /// Renders the "reset your password" mail with the link built around
    /// <paramref name="rawToken"/> and ships it. Surfaces
    /// <see cref="EmailDeliveryException"/> on SMTP failure; the caller
    /// (AuthService.ForgotPasswordAsync) catches and logs to keep the
    /// endpoint anti-enumeration.
    /// </summary>
    Task SendAsync(User user, string rawToken, CancellationToken ct = default);
}

public class PasswordResetSender : IPasswordResetSender
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly PasswordResetOptions _options;

    public PasswordResetSender(
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer,
        IOptions<PasswordResetOptions> options)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _options = options.Value;
    }

    public async Task SendAsync(User user, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var html = _renderer.Render("password-reset", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["resetLink"] = link,
            ["tokenLifetimeMinutes"] = ((int)_options.TokenLifetime.TotalMinutes).ToString(),
        }, user.PreferredLocale);

        await _emailSender.SendAsync(
            to: user.Email,
            subject: LocalisedSubject(user.PreferredLocale),
            htmlBody: html,
            ct: ct);
    }

    private static string LocalisedSubject(string? locale) => (locale?.ToLowerInvariant()) switch
    {
        "en" => "Reset your password — Althea Systems",
        "ms" => "Set semula kata laluan anda — Althea Systems",
        "ar" => "إعادة تعيين كلمة المرور — Althea Systems",
        _ => "Réinitialisation de votre mot de passe — Althea Systems",
    };
}
