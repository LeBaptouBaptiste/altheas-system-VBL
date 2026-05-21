using System.Globalization;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API_Althea_systems.Services;

/// <summary>
/// PDF renderer for Invoices. Renders a French-style invoice (or credit note)
/// with our brand colors and the legal mentions required for B2B sales of
/// medical equipment.
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    private const string BrandPrimary = "#0F4C81";
    private const string BrandGreen = "#2E8B57";
    private const string MutedText = "#6B7280";
    private const string LightGrey = "#F3F4F6";

    // Issuer info — would be read from config (CompanyInfo section) in a
    // multi-tenant deployment. Hardcoded here since Althea Systems is the
    // single seller for the V1 monomerchant model.
    private const string IssuerName = "Althea Systems SAS";
    private const string IssuerAddress = "1 rue de la Santé, 75013 Paris";
    private const string IssuerSiret = "SIRET : 123 456 789 00012";
    private const string IssuerTva = "TVA : FR12 345678900";
    private const string IssuerContact = "contact@altheasystems.com · +33 1 23 45 67 89";

    private static readonly CultureInfo FrCulture = new("fr-FR");

    public byte[] Render(Invoice invoice, Order order, User user, Address billingAddress)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(billingAddress);
        if (order.Items is null || order.Items.Count == 0)
            throw new ArgumentException("Order has no items — cannot render an invoice.", nameof(order));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1F2937"));

                page.Header().Element(c => ComposeHeader(c, invoice));
                page.Content().Element(c => ComposeContent(c, invoice, order, user, billingAddress));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice)
    {
        var isCreditNote = invoice.Type == InvoiceType.CreditNote;
        var title = isCreditNote ? "AVOIR" : "FACTURE";
        var accentColor = isCreditNote ? "#DC2626" : BrandPrimary;

        container.Row(row =>
        {
            // Left: brand block
            row.RelativeItem().Column(col =>
            {
                col.Item().Row(brand =>
                {
                    brand.AutoItem().Background(BrandPrimary)
                        .Width(40).Height(40).AlignCenter().AlignMiddle()
                        .Text("A").FontSize(24).Bold().FontColor(Colors.White);
                    brand.AutoItem().PaddingLeft(10).AlignMiddle()
                        .Text("Althea Systems").FontSize(18).Bold().FontColor(BrandPrimary);
                });
                col.Item().PaddingTop(8).Text("Matériel médical professionnel")
                    .FontSize(9).FontColor(MutedText).Italic();
            });

            // Right: invoice metadata
            row.ConstantItem(200).AlignRight().Column(col =>
            {
                col.Item().Text(title).FontSize(28).Bold().FontColor(accentColor);
                col.Item().PaddingTop(4)
                    .Text($"N° {invoice.Number}")
                    .FontSize(11).FontColor(MutedText);
                col.Item().Text($"Émise le {invoice.Date.ToString("dd/MM/yyyy", FrCulture)}")
                    .FontSize(10).FontColor(MutedText);
                if (isCreditNote && invoice.RelatedInvoiceId.HasValue)
                {
                    // Prefer the human-readable Number of the original invoice
                    // when EF has loaded the RelatedInvoice navigation. Fall
                    // back to a short Guid extract if not (defensive — every
                    // caller in the codebase Includes it today).
                    var relatedDisplay = !string.IsNullOrEmpty(invoice.RelatedInvoice?.Number)
                        ? invoice.RelatedInvoice.Number
                        : invoice.RelatedInvoiceId.Value.ToString()[..8].ToUpperInvariant();
                    col.Item().PaddingTop(2)
                        .Text($"Réf. facture {relatedDisplay}")
                        .FontSize(9).FontColor(MutedText);
                }
            });
        });
    }

    private static void ComposeContent(
        IContainer container,
        Invoice invoice,
        Order order,
        User user,
        Address billing)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Spacing(15);

            // Issuer / customer block — side by side
            col.Item().Row(row =>
            {
                row.RelativeItem().Background(LightGrey).Padding(12).Column(c =>
                {
                    c.Item().Text("Émetteur").FontSize(9).SemiBold().FontColor(MutedText);
                    c.Item().PaddingTop(4).Text(IssuerName).Bold();
                    c.Item().Text(IssuerAddress);
                    c.Item().PaddingTop(4).Text(IssuerSiret).FontSize(9).FontColor(MutedText);
                    c.Item().Text(IssuerTva).FontSize(9).FontColor(MutedText);
                });

                row.ConstantItem(15);

                row.RelativeItem().Background(LightGrey).Padding(12).Column(c =>
                {
                    c.Item().Text("Facturé à").FontSize(9).SemiBold().FontColor(MutedText);
                    c.Item().PaddingTop(4).Text($"{billing.FirstName} {billing.LastName}").Bold();
                    if (!string.IsNullOrEmpty(billing.Company))
                        c.Item().Text(billing.Company);
                    c.Item().Text(billing.Street);
                    if (!string.IsNullOrEmpty(billing.Street2))
                        c.Item().Text(billing.Street2!);
                    c.Item().Text($"{billing.PostalCode} {billing.City}");
                    c.Item().Text(billing.Country);
                    c.Item().PaddingTop(4).Text(user.Email).FontSize(9).FontColor(MutedText);
                });
            });

            // Order reference strip
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Commande : ").SemiBold().FontColor(MutedText);
                    text.Span(order.Id.ToString()[..8].ToUpperInvariant()).FontFamily(Fonts.Consolas);
                });
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Date commande : ").SemiBold().FontColor(MutedText);
                    text.Span(order.Date.ToString("dd/MM/yyyy", FrCulture));
                });
            });

            // Line items
            col.Item().Element(c => ComposeItemsTable(c, order));

            // Totals
            col.Item().AlignRight().Element(c => ComposeTotals(c, invoice, order));

            // Payment status
            col.Item().PaddingTop(10).Element(c => ComposeStatus(c, invoice));
        });
    }

    private static void ComposeItemsTable(IContainer container, Order order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(5);  // Product
                c.RelativeColumn(1);  // Qty
                c.RelativeColumn(2);  // Unit HT
                c.RelativeColumn(1);  // VAT %
                c.RelativeColumn(2);  // Line HT
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(BrandPrimary).Padding(6).Text("Produit").FontColor(Colors.White).SemiBold().FontSize(9);
                header.Cell().Background(BrandPrimary).Padding(6).AlignCenter().Text("Qté").FontColor(Colors.White).SemiBold().FontSize(9);
                header.Cell().Background(BrandPrimary).Padding(6).AlignRight().Text("PU HT").FontColor(Colors.White).SemiBold().FontSize(9);
                header.Cell().Background(BrandPrimary).Padding(6).AlignCenter().Text("TVA").FontColor(Colors.White).SemiBold().FontSize(9);
                header.Cell().Background(BrandPrimary).Padding(6).AlignRight().Text("Total HT").FontColor(Colors.White).SemiBold().FontSize(9);
            });

            // Rows
            var i = 0;
            foreach (var item in order.Items)
            {
                // Both branches as hex strings: QuestPDF's Colors.* are typed
                // as Color, mixing with our hex-string LightGrey breaks the
                // ternary type inference.
                var rowBg = i++ % 2 == 0 ? "#FFFFFF" : LightGrey;
                var lineHt = item.PriceHT * item.Quantity;
                table.Cell().Background(rowBg).Padding(6).Text(item.ProductNameFr);
                table.Cell().Background(rowBg).Padding(6).AlignCenter().Text(item.Quantity.ToString(FrCulture));
                table.Cell().Background(rowBg).Padding(6).AlignRight().Text(FormatEur(item.PriceHT));
                table.Cell().Background(rowBg).Padding(6).AlignCenter().Text(VatLabel(item.VatRate)).FontSize(9);
                table.Cell().Background(rowBg).Padding(6).AlignRight().Text(FormatEur(lineHt)).SemiBold();
            }
        });
    }

    private static void ComposeTotals(IContainer container, Invoice invoice, Order order)
    {
        // Only show the payment-breakdown section on real invoices, not on
        // credit notes (an avoir doesn't itself receive a payment — its
        // own ventilation is in the original invoice).
        var isInvoice = invoice.Type == InvoiceType.Invoice;
        var creditAppliedEur = order.CreditAppliedCents / 100m;

        container.MaxWidth(260).Padding(0).Column(col =>
        {
            col.Spacing(4);

            // Subtotal HT
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Sous-total HT").FontColor(MutedText);
                r.ConstantItem(110).AlignRight().Text(FormatEur(invoice.AmountHT));
            });

            // Shipping (if any)
            if (order.ShippingCost > 0)
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Frais de livraison").FontColor(MutedText);
                    r.ConstantItem(110).AlignRight().Text(FormatEur(order.ShippingCost));
                });
            }

            // VAT
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("TVA").FontColor(MutedText);
                r.ConstantItem(110).AlignRight().Text(FormatEur(invoice.VatAmount));
            });

            // Separator + total TTC
            col.Item().LineHorizontal(0.5f).LineColor(MutedText);
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Total TTC").Bold().FontSize(13).FontColor(BrandPrimary);
                r.ConstantItem(110).AlignRight().Text(FormatEur(invoice.AmountTTC)).Bold().FontSize(13).FontColor(BrandPrimary);
            });

            // Phase 7: payment-method breakdown when store credit was
            // applied at checkout. Comptablement obligatoire — la facture
            // doit montrer comment l'argent a réellement été reçu (avoir
            // consommé vs Stripe). Skipped on credit notes (Type != Invoice)
            // and on orders paid 100% by card.
            if (isInvoice && creditAppliedEur > 0)
            {
                col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(MutedText);
                col.Item().PaddingTop(4).Text("Détail du règlement").FontSize(9).FontColor(MutedText);

                // Avoir consommé (negative)
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Avoir appliqué").FontColor(MutedText);
                    r.ConstantItem(110).AlignRight()
                        .Text("− " + FormatEur(creditAppliedEur))
                        .FontColor("#DC2626");
                });

                // Reste payé via Stripe / autre méthode
                var stripePaidEur = invoice.AmountTTC - creditAppliedEur;
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(PaymentMethodLabel(order.PaymentMethod)).FontColor(MutedText);
                    r.ConstantItem(110).AlignRight().Text(FormatEur(stripePaidEur)).SemiBold();
                });
            }
        });
    }

    /// <summary>
    /// Human-readable payment-method label for the PDF. Falls back to the
    /// enum name if we forget to map a new value — better than crashing.
    /// </summary>
    private static string PaymentMethodLabel(Common.Enums.PaymentMethod method) => method switch
    {
        Common.Enums.PaymentMethod.Card => "Réglé par carte bancaire",
        Common.Enums.PaymentMethod.BankTransfer => "Réglé par virement bancaire",
        Common.Enums.PaymentMethod.AdminMandate => "Réglé sur mandat administratif",
        _ => $"Réglé ({method})",
    };

    private static void ComposeStatus(IContainer container, Invoice invoice)
    {
        var (label, bg, fg) = invoice.Status switch
        {
            InvoiceStatus.Paid => ("Payée", "#D1FAE5", "#065F46"),
            InvoiceStatus.Pending => ("En attente de paiement", "#FEF3C7", "#92400E"),
            InvoiceStatus.Overdue => ("En retard", "#FEE2E2", "#991B1B"),
            InvoiceStatus.Cancelled => ("Annulée", "#E5E7EB", "#374151"),
            _ => (invoice.Status.ToString(), "#E5E7EB", "#374151"),
        };

        container.Background(bg).Padding(10).Row(r =>
        {
            r.AutoItem().Text("Statut :").Bold().FontColor(fg);
            r.AutoItem().PaddingLeft(8).Text(label).FontColor(fg);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(10).BorderTop(0.5f).BorderColor(MutedText).PaddingTop(8).Column(col =>
        {
            col.Spacing(2);
            col.Item().Text("Conditions de paiement : 30 jours net.").FontSize(8).FontColor(MutedText);
            col.Item().Text("Tout retard de paiement entraînera des pénalités calculées au taux d'intérêt légal majoré.").FontSize(8).FontColor(MutedText);
            col.Item().Text(IssuerContact).FontSize(8).FontColor(MutedText);
            col.Item().AlignRight().Text(text =>
            {
                text.CurrentPageNumber().FontSize(8).FontColor(MutedText);
                text.Span(" / ").FontSize(8).FontColor(MutedText);
                text.TotalPages().FontSize(8).FontColor(MutedText);
            });
        });
    }

    private static string FormatEur(decimal amount) =>
        amount.ToString("C2", FrCulture);

    private static string VatLabel(VatRate rate) => rate switch
    {
        VatRate.Standard => "20%",
        VatRate.Intermediate => "10%",
        VatRate.Reduced => "5,5%",
        VatRate.Zero => "0%",
        _ => "—",
    };
}
