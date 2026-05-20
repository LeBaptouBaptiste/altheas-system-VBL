using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 4b: tells the sender which copy to use. Setup codes go out during
/// the one-time activation flow ("voici votre code pour activer la 2FA par
/// email"). Login codes are sent at every authentication when the user has
/// chosen the Email method ("votre code de connexion à usage unique").
/// </summary>
public enum TwoFactorCodePurpose
{
    Setup,
    Login,
}

public interface ITwoFactorCodeSender
{
    /// <summary>
    /// Renders and ships the email containing the 6-digit code. Surfaces
    /// <see cref="EmailDeliveryException"/> — the caller (TwoFactorService)
    /// has already persisted the code's hash in Redis; if the mail fails
    /// it's the caller's job to decide whether to roll back the Redis state.
    /// </summary>
    Task SendAsync(User user, string code, TwoFactorCodePurpose purpose,
        TimeSpan ttl, CancellationToken ct = default);
}

public class TwoFactorCodeSender : ITwoFactorCodeSender
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;

    public TwoFactorCodeSender(IEmailSender emailSender, IEmailTemplateRenderer renderer)
    {
        _emailSender = emailSender;
        _renderer = renderer;
    }

    public async Task SendAsync(
        User user,
        string code,
        TwoFactorCodePurpose purpose,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var (subject, purposeLine) = purpose switch
        {
            TwoFactorCodePurpose.Setup => (
                "Activez la 2FA par email — Althea Systems",
                "Voici le code à usage unique pour activer l'authentification à deux facteurs par email sur votre compte."),
            TwoFactorCodePurpose.Login => (
                "Code de connexion — Althea Systems",
                "Voici votre code de connexion à usage unique. Saisissez-le sur la page de connexion pour finaliser l'authentification."),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose,
                "Unknown 2FA code purpose."),
        };

        var html = _renderer.Render("2fa-code", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["code"] = code,
            ["purposeLine"] = purposeLine,
            ["ttlMinutes"] = ((int)ttl.TotalMinutes).ToString(),
        });

        await _emailSender.SendAsync(
            to: user.Email,
            subject: subject,
            htmlBody: html,
            ct: ct);
    }
}
