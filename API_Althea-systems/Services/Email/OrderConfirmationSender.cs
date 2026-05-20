using System.Globalization;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 3: builds the "thanks for your order" email with the PDF invoice
/// attached, and ships it out via <see cref="IEmailSender"/>.
///
/// Idempotency is NOT enforced here — the sender just sends. The caller
/// (<see cref="InvoiceService.EnsureEmailedAsync"/>) gates on
/// <see cref="Invoice.EmailedAt"/>.
/// </summary>
public interface IOrderConfirmationSender
{
    /// <summary>
    /// Renders the PDF + sends the receipt email. The caller MUST have
    /// pre-loaded order navigation properties (Items, BillingAddress) — the
    /// PDF service throws otherwise. Surfaces
    /// <see cref="EmailDeliveryException"/> on SMTP failure so the caller
    /// can decide whether to roll back its idempotency mark.
    /// </summary>
    Task SendAsync(Invoice invoice, Order order, User user, CancellationToken ct = default);
}

public class OrderConfirmationSender : IOrderConfirmationSender
{
    // Per-customer currency. We render French totals (1 234,56 €) because
    // the audience is FR-speaking medical pros. When we add i18n support to
    // emails (phase 5?), thread the locale through here.
    private static readonly CultureInfo FrFr = new("fr-FR");

    private readonly IInvoicePdfService _pdf;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;

    public OrderConfirmationSender(
        IInvoicePdfService pdf,
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer)
    {
        _pdf = pdf;
        _emailSender = emailSender;
        _renderer = renderer;
    }

    public async Task SendAsync(Invoice invoice, Order order, User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(user);

        // BillingAddress is required by the PDF renderer. Fail loud rather
        // than ship a PDF with blank seller block, which would look unprofessional.
        if (order.BillingAddress is null)
        {
            throw new InvalidOperationException(
                $"Cannot send order confirmation: order {order.Id} has no BillingAddress loaded. " +
                "The OrderRepository must Include(o.BillingAddress) for this call site.");
        }

        var pdfBytes = _pdf.Render(invoice, order, user, order.BillingAddress);
        var attachment = new EmailAttachment(
            FileName: $"facture-{invoice.Id.ToString("N")[..8].ToUpperInvariant()}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        // Short, friendly recap for the email body. The PDF carries the full
        // line-by-line breakdown.
        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var html = _renderer.Render("order-confirmation", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["orderShortId"] = order.Id.ToString("N")[..8].ToUpperInvariant(),
            ["orderDate"] = order.Date.ToString("dd MMMM yyyy", FrFr),
            ["itemCount"] = order.Items.Count.ToString(),
            ["totalHT"] = FormatEur(invoice.AmountHT),
            ["totalVAT"] = FormatEur(invoice.VatAmount),
            ["totalTTC"] = FormatEur(invoice.AmountTTC),
            ["shippingCost"] = FormatEur(order.ShippingCost),
        });

        await _emailSender.SendAsync(
            to: user.Email,
            subject: $"Confirmation de commande #{order.Id.ToString("N")[..8].ToUpperInvariant()} — Althea Systems",
            htmlBody: html,
            attachments: new[] { attachment },
            ct: ct);
    }

    private static string FormatEur(decimal amount) =>
        amount.ToString("N2", FrFr) + " €";
}
