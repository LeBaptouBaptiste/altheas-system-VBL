using API_Althea_systems.Services.Email;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// One-method facade over SMTP. Implementations should:
///   - Be safe to call from a scoped or singleton context (no shared state).
///   - Throw <see cref="EmailDeliveryException"/> on transport / delivery failures
///     and let the caller decide whether to surface or swallow.
///   - Honor the cancellation token (long SMTP handshakes are common).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an HTML email to a single recipient. Attachments are optional and
    /// passed by value (see <see cref="EmailAttachment"/>) so the caller can
    /// dispose its source stream before the send completes.
    /// </summary>
    /// <exception cref="EmailDeliveryException">SMTP rejected or could not reach the server.</exception>
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default);
}
