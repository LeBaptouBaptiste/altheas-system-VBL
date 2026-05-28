using System.Text;
using FluentAssertions;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services;
using QuestPDF.Infrastructure;

namespace API_Althea_systems.Tests.Services;

/// <summary>
/// Sanity checks for the PDF renderer. We can't easily diff binary PDFs,
/// so we cover : (1) the output is a valid PDF (header magic),
/// (2) reasonable size (≥ 1 KB means actual content, not an empty doc),
/// (3) input-validation rejects malformed aggregates.
/// </summary>
public class InvoicePdfServiceTests
{
    public InvoicePdfServiceTests()
    {
        // QuestPDF refuses to render without an explicit license. Tests run
        // in isolation from Program.cs so we set it here too.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static (Invoice invoice, Order order, User user, Address billing) BuildFixture(
        InvoiceType type = InvoiceType.Invoice,
        InvoiceStatus status = InvoiceStatus.Paid)
    {
        var userId = Guid.NewGuid();
        var addr = new Address
        {
            Id = Guid.NewGuid(), UserId = userId, Label = "Facturation",
            FirstName = "Sophie", LastName = "Martin", Company = "Hôpital de Lyon",
            Street = "10 rue de la Santé", City = "Lyon", PostalCode = "69001",
            Country = "France",
        };
        var order = new Order
        {
            Id = Guid.NewGuid(), UserId = userId, Date = new DateTime(2026, 5, 1),
            Status = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Validated,
            PaymentMethod = PaymentMethod.Card, BillingAddressId = addr.Id, ShippingAddressId = addr.Id,
            ShippingMethod = ShippingMethod.Standard, ShippingCost = 15m,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(), ProductId = Guid.NewGuid(),
                    ProductNameFr = "Stéthoscope Littmann", ProductNameEn = "Littmann stethoscope",
                    Quantity = 2, PriceHT = 80m, VatRate = VatRate.Standard,
                },
                new OrderItem
                {
                    Id = Guid.NewGuid(), ProductId = Guid.NewGuid(),
                    ProductNameFr = "Tensiomètre numérique", ProductNameEn = "Digital BP monitor",
                    Quantity = 1, PriceHT = 120m, VatRate = VatRate.Standard,
                },
            ],
        };
        var user = new User
        {
            Id = userId, Name = "Sophie Martin", Email = "sophie@hopital-lyon.fr",
            PasswordHash = "x", Role = UserRole.Customer, Status = UserStatus.Active,
            Addresses = [addr],
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrderId = order.Id, Date = order.Date,
            AmountHT = 280m, VatAmount = 56m, AmountTTC = 351m,  // includes 15 shipping
            Status = status, Type = type,
        };
        return (invoice, order, user, addr);
    }

    [Fact]
    public void Render_ValidInvoice_ProducesPdfBytes()
    {
        var sut = new InvoicePdfService();
        var (invoice, order, user, billing) = BuildFixture();

        var pdf = sut.Render(invoice, order, user, billing);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(1024, "a real invoice PDF is at least a few KB");

        // PDFs always start with the magic bytes "%PDF-"
        var magic = Encoding.ASCII.GetString(pdf.AsSpan(0, 5));
        magic.Should().Be("%PDF-");
    }

    [Fact]
    public void Render_CreditNote_StillProducesValidPdf()
    {
        // The credit note branch uses a different title + accent color but
        // shares the rest of the layout. We just check it doesn't blow up.
        var sut = new InvoicePdfService();
        var (invoice, order, user, billing) = BuildFixture(type: InvoiceType.CreditNote);
        invoice.RelatedInvoiceId = Guid.NewGuid();

        var pdf = sut.Render(invoice, order, user, billing);

        pdf.Length.Should().BeGreaterThan(1024);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void Render_AllStatuses_DoesNotThrow(InvoiceStatus status)
    {
        var sut = new InvoicePdfService();
        var (invoice, order, user, billing) = BuildFixture(status: status);

        var act = () => sut.Render(invoice, order, user, billing);

        act.Should().NotThrow();
    }

    [Fact]
    public void Render_OrderWithoutItems_Throws()
    {
        // The renderer requires at least one line item — a blank invoice is
        // almost certainly a bug upstream.
        var sut = new InvoicePdfService();
        var (invoice, order, user, billing) = BuildFixture();
        order.Items.Clear();

        var act = () => sut.Render(invoice, order, user, billing);

        act.Should().Throw<ArgumentException>().WithMessage("*no items*");
    }

    [Fact]
    public void Render_NullInputs_ThrowArgumentNullException()
    {
        var sut = new InvoicePdfService();
        var (invoice, order, user, billing) = BuildFixture();

        ((Action)(() => sut.Render(null!, order, user, billing))).Should().Throw<ArgumentNullException>();
        ((Action)(() => sut.Render(invoice, null!, user, billing))).Should().Throw<ArgumentNullException>();
        ((Action)(() => sut.Render(invoice, order, null!, billing))).Should().Throw<ArgumentNullException>();
        ((Action)(() => sut.Render(invoice, order, user, null!))).Should().Throw<ArgumentNullException>();
    }
}
