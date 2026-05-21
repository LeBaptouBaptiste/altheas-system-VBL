using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 4a: machine-readable kind of security alert. The sender picks
/// subject + body copy based on this enum so call sites don't have to
/// hand-write the strings.
/// </summary>
public enum SecurityAlertType
{
    TwoFactorEnabled,
    TwoFactorDisabled,
    RecoveryCodesRegenerated,
}

/// <summary>
/// Sends "this sensitive action happened on your account" notifications.
/// Distinct from <see cref="IEmailConfirmationSender"/> and
/// <see cref="IOrderConfirmationSender"/> so each transactional email
/// type owns its own copy / placeholders without entangling them.
///
/// Best-effort by contract: callers wrap in try/catch and never let an
/// SMTP failure roll back the underlying security operation (the action
/// already happened in the DB; the alert is a heads-up, not a transaction).
/// </summary>
public interface ISecurityAlertSender
{
    Task SendAsync(User user, SecurityAlertType type, CancellationToken ct = default);
}

public class SecurityAlertSender : ISecurityAlertSender
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly ILogger<SecurityAlertSender> _logger;

    public SecurityAlertSender(
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer,
        ILogger<SecurityAlertSender> logger)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task SendAsync(User user, SecurityAlertType type, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var (subject, title, message) = CopyFor(type, user.PreferredLocale);
        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var html = _renderer.Render("security-alert", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["title"] = title,
            ["message"] = message,
            // UTC instant the alert is being sent — close enough to the
            // event itself (we fire synchronously after the action).
            ["when"] = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'"),
        }, user.PreferredLocale);

        await _emailSender.SendAsync(
            to: user.Email,
            subject: subject,
            htmlBody: html,
            ct: ct);

        _logger.LogInformation(
            "Security alert {AlertType} sent to user {UserId} ({Email}).",
            type, user.Id, user.Email);
    }

    /// <summary>
    /// Single source of truth for the (subject, title, message) triplet of
    /// each alert, localised to the user's preferred locale. Keep the copy
    /// here rather than in the template so the template stays generic and
    /// one file covers every alert type × every locale.
    /// </summary>
    private static (string Subject, string Title, string Message) CopyFor(
        SecurityAlertType type, string? locale)
        => (type, locale?.ToLowerInvariant()) switch
        {
            // ── English ─────────────────────────────────
            (SecurityAlertType.TwoFactorEnabled, "en") => (
                Subject: "Two-factor authentication enabled — Althea Systems",
                Title: "Two-factor authentication enabled",
                Message: "Two-factor authentication has just been enabled on your account. From now on you'll need to enter a 6-digit code from your authenticator app at every sign-in."),
            (SecurityAlertType.TwoFactorDisabled, "en") => (
                Subject: "Two-factor authentication disabled — Althea Systems",
                Title: "Two-factor authentication disabled",
                Message: "Two-factor authentication has just been disabled on your account. Your account is now protected by your password alone."),
            (SecurityAlertType.RecoveryCodesRegenerated, "en") => (
                Subject: "Recovery codes regenerated — Althea Systems",
                Title: "Recovery codes regenerated",
                Message: "A new set of recovery codes has just been generated for your account. The old codes no longer work."),

            // ── Bahasa Melayu ──────────────────────────
            (SecurityAlertType.TwoFactorEnabled, "ms") => (
                Subject: "Pengesahan dua faktor diaktifkan — Althea Systems",
                Title: "Pengesahan dua faktor diaktifkan",
                Message: "Pengesahan dua faktor baru sahaja diaktifkan pada akaun anda. Mulai sekarang anda perlu memasukkan kod 6 digit dari aplikasi pengesah anda pada setiap log masuk."),
            (SecurityAlertType.TwoFactorDisabled, "ms") => (
                Subject: "Pengesahan dua faktor dinyahaktifkan — Althea Systems",
                Title: "Pengesahan dua faktor dinyahaktifkan",
                Message: "Pengesahan dua faktor baru sahaja dinyahaktifkan pada akaun anda. Akaun anda kini dilindungi oleh kata laluan anda sahaja."),
            (SecurityAlertType.RecoveryCodesRegenerated, "ms") => (
                Subject: "Kod pemulihan dijana semula — Althea Systems",
                Title: "Kod pemulihan dijana semula",
                Message: "Satu set baru kod pemulihan baru sahaja dijana untuk akaun anda. Kod-kod lama tidak lagi berfungsi."),

            // ── العربية ────────────────────────────────
            (SecurityAlertType.TwoFactorEnabled, "ar") => (
                Subject: "تم تفعيل المصادقة الثنائية — Althea Systems",
                Title: "تم تفعيل المصادقة الثنائية",
                Message: "تم للتو تفعيل المصادقة الثنائية على حسابك. من الآن فصاعداً، عليك إدخال رمز مكوّن من 6 أرقام من تطبيق المصادقة عند كل تسجيل دخول."),
            (SecurityAlertType.TwoFactorDisabled, "ar") => (
                Subject: "تم تعطيل المصادقة الثنائية — Althea Systems",
                Title: "تم تعطيل المصادقة الثنائية",
                Message: "تم للتو تعطيل المصادقة الثنائية على حسابك. أصبح حسابك الآن محمياً بكلمة المرور فقط."),
            (SecurityAlertType.RecoveryCodesRegenerated, "ar") => (
                Subject: "تم إعادة إنشاء رموز الاسترداد — Althea Systems",
                Title: "تم إعادة إنشاء رموز الاسترداد",
                Message: "تم للتو إنشاء مجموعة جديدة من رموز الاسترداد لحسابك. لم تعد الرموز القديمة صالحة."),

            // ── Français (default + fallback) ─────────
            (SecurityAlertType.TwoFactorEnabled, _) => (
                Subject: "Authentification à deux facteurs activée — Althea Systems",
                Title: "Authentification à deux facteurs activée",
                Message: "L'authentification à deux facteurs vient d'être activée sur votre compte. Vous devrez désormais saisir un code à 6 chiffres généré par votre application authentificatrice à chaque connexion."),
            (SecurityAlertType.TwoFactorDisabled, _) => (
                Subject: "Authentification à deux facteurs désactivée — Althea Systems",
                Title: "Authentification à deux facteurs désactivée",
                Message: "L'authentification à deux facteurs vient d'être désactivée sur votre compte. Votre compte est désormais protégé uniquement par votre mot de passe."),
            (SecurityAlertType.RecoveryCodesRegenerated, _) => (
                Subject: "Codes de récupération régénérés — Althea Systems",
                Title: "Codes de récupération régénérés",
                Message: "Une nouvelle série de codes de récupération vient d'être générée pour votre compte. Les anciens codes ne fonctionnent plus."),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown security alert type."),
        };
}
