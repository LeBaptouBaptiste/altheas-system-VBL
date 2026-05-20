using System.Globalization;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 6: when an admin issues a credit note (<see cref="InvoiceType.CreditNote"/>)
/// against a paid invoice, we notify the customer with the PDF attached.
/// Mirrors <see cref="IOrderConfirmationSender"/> structurally, separate
/// interface so the copy / subject stay distinct from "thanks for ordering".
/// </summary>
public interface ICreditNoteSender
{
    /// <summary>
    /// Renders the credit-note PDF (already supported by InvoicePdfService —
    /// title "AVOIR", red accent), attaches it, and sends.
    /// Throws <see cref="EmailDeliveryException"/> on SMTP failure; the caller
    /// (InvoiceService.IssueCreditNoteAsync) catches so a flaky SMTP doesn't
    /// roll back the credit note that's already in the DB.
    /// </summary>
    Task SendAsync(
        Invoice creditNote,
        Invoice originalInvoice,
        Order order,
        User user,
        string? reason,
        CancellationToken ct = default);
}

public class CreditNoteSender : ICreditNoteSender
{
    private static readonly CultureInfo FrFr = new("fr-FR");

    private readonly IInvoicePdfService _pdf;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;

    public CreditNoteSender(
        IInvoicePdfService pdf,
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer)
    {
        _pdf = pdf;
        _emailSender = emailSender;
        _renderer = renderer;
    }

    public async Task SendAsync(
        Invoice creditNote,
        Invoice originalInvoice,
        Order order,
        User user,
        string? reason,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(creditNote);
        ArgumentNullException.ThrowIfNull(originalInvoice);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(user);

        if (order.BillingAddress is null)
        {
            throw new InvalidOperationException(
                $"Cannot send credit note: order {order.Id} has no BillingAddress loaded. " +
                "Caller must Include(o.BillingAddress).");
        }

        var pdfBytes = _pdf.Render(creditNote, order, user, order.BillingAddress);
        var attachment = new EmailAttachment(
            FileName: $"avoir-{creditNote.Id.ToString("N")[..8].ToUpperInvariant()}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var html = _renderer.Render("credit-note-issued", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["creditNoteShortId"] = creditNote.Id.ToString("N")[..8].ToUpperInvariant(),
            ["originalInvoiceShortId"] = originalInvoice.Id.ToString("N")[..8].ToUpperInvariant(),
            ["orderShortId"] = order.Id.ToString("N")[..8].ToUpperInvariant(),
            ["creditNoteDate"] = creditNote.Date.ToString("dd MMMM yyyy", FrFr),
            ["amountTTC"] = FormatEur(creditNote.AmountTTC),
            // Reason is optional — if blank we render a fallback line.
            ["reason"] = string.IsNullOrWhiteSpace(reason)
                ? "Avoir émis par notre service comptable."
                : reason,
        });

        await _emailSender.SendAsync(
            to: user.Email,
            subject: $"Avoir #{creditNote.Id.ToString("N")[..8].ToUpperInvariant()} émis — Althea Systems",
            htmlBody: html,
            attachments: new[] { attachment },
            ct: ct);
    }

    private static string FormatEur(decimal amount) =>
        amount.ToString("N2", FrFr) + " €";
}
