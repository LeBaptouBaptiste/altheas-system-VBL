using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API_Althea_systems.Tests.Services;

/// <summary>
/// Focused tests for <see cref="InvoiceService.EnsureEmailedAsync"/> — the
/// Stripe webhook redelivery contract relies on its idempotency, so we lock
/// in the gate logic here even though the happy-path PDF rendering is
/// covered indirectly via the OrderConfirmationSender's own dependencies.
/// </summary>
public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IOrderConfirmationSender> _sender = new();
    private readonly Mock<ICreditNoteSender> _creditNoteSender = new();
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        // Default: no prior credit notes against any invoice. Specific tests
        // override to exercise the cumulative-cap branch.
        _invoiceRepo.Setup(r => r.GetCreditNotesForInvoiceAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(Enumerable.Empty<Invoice>());

        _sut = new InvoiceService(
            _invoiceRepo.Object,
            _orderRepo.Object,
            _sender.Object,
            _creditNoteSender.Object,
            NullLogger<InvoiceService>.Instance);
    }

    private static Order MakeOrder(
        Guid id,
        IEnumerable<Invoice>? invoices = null,
        User? user = null)
        => new()
        {
            Id = id,
            UserId = user?.Id ?? Guid.NewGuid(),
            User = user ?? new User { Id = Guid.NewGuid(), Email = "c@x.com", Name = "Client", PasswordHash = "" },
            Date = DateTime.UtcNow,
            PaymentStatus = PaymentStatus.Validated,
            ShippingMethod = ShippingMethod.Standard,
            BillingAddressId = Guid.NewGuid(),
            ShippingAddressId = Guid.NewGuid(),
            Items = [],
            Invoices = invoices?.ToList() ?? [],
        };

    private static Invoice MakeInvoice(Guid orderId, DateTime? emailedAt = null, InvoiceType type = InvoiceType.Invoice)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Date = DateTime.UtcNow,
            Type = type,
            Status = InvoiceStatus.Paid,
            EmailedAt = emailedAt,
        };

    [Fact]
    public async Task EnsureEmailedAsync_NotYetSent_RendersSends_AndMarksEmailedAt()
    {
        var orderId = Guid.NewGuid();
        var invoice = MakeInvoice(orderId);
        var order = MakeOrder(orderId, invoices: [invoice]);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.EnsureEmailedAsync(orderId);

        _sender.Verify(s => s.SendAsync(invoice, order, order.User, It.IsAny<CancellationToken>()),
            Times.Once);
        invoice.EmailedAt.Should().NotBeNull();
        _invoiceRepo.Verify(r => r.UpdateAsync(invoice), Times.Once);
    }

    [Fact]
    public async Task EnsureEmailedAsync_AlreadyEmailed_IsNoOp()
    {
        // Stripe redelivery / boot retry → we must NOT email twice.
        var orderId = Guid.NewGuid();
        var alreadySent = DateTime.UtcNow.AddHours(-1);
        var invoice = MakeInvoice(orderId, emailedAt: alreadySent);
        var order = MakeOrder(orderId, invoices: [invoice]);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.EnsureEmailedAsync(orderId);

        _sender.Verify(s => s.SendAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
        invoice.EmailedAt.Should().Be(alreadySent, "EmailedAt must not be bumped on a no-op");
        _invoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailedAsync_OrderHasNoInvoice_LogsAndSkips()
    {
        // EnsureForOrderAsync should have run first. If somehow it didn't,
        // we don't crash the webhook — just skip and log.
        var orderId = Guid.NewGuid();
        var order = MakeOrder(orderId, invoices: []);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.EnsureEmailedAsync(orderId);

        _sender.Verify(s => s.SendAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _invoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailedAsync_OnlyCreditNote_IsSkipped()
    {
        // Credit notes don't count as the order's primary invoice — we
        // never email "thanks for your refund" from this path.
        var orderId = Guid.NewGuid();
        var creditNote = MakeInvoice(orderId, type: InvoiceType.CreditNote);
        var order = MakeOrder(orderId, invoices: [creditNote]);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.EnsureEmailedAsync(orderId);

        _sender.Verify(s => s.SendAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailedAsync_OrderNotFound_LogsAndSkips()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var act = () => _sut.EnsureEmailedAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
        _sender.Verify(s => s.SendAsync(It.IsAny<Invoice>(), It.IsAny<Order>(), It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailedAsync_SendFails_DoesNotMarkEmailedAt_AndPropagates()
    {
        // Critical: if the send fails, EmailedAt must STAY null so the next
        // retry (Stripe webhook redelivery, manual admin re-trigger, future
        // backfill) can attempt again. We also propagate so the caller's
        // outer try/catch logs the original cause.
        var orderId = Guid.NewGuid();
        var invoice = MakeInvoice(orderId);
        var order = MakeOrder(orderId, invoices: [invoice]);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _sender
            .Setup(s => s.SendAsync(invoice, order, order.User, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));

        var act = () => _sut.EnsureEmailedAsync(orderId);

        await act.Should().ThrowAsync<EmailDeliveryException>();
        invoice.EmailedAt.Should().BeNull("a failed send must NOT mark the invoice as emailed");
        _invoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailedAsync_PicksMostRecentTypeInvoice_WhenMultipleExist()
    {
        // Defensive: if an order somehow ends up with two Type=Invoice rows
        // (admin re-issue, race during Ensure), we email the most recent —
        // matches LatestInvoiceId selection in OrderService.MapToDto so the
        // customer's "Download" button and the mailed PDF stay consistent.
        var orderId = Guid.NewGuid();
        var older = MakeInvoice(orderId);
        older.Date = DateTime.UtcNow.AddDays(-5);
        var newer = MakeInvoice(orderId);
        newer.Date = DateTime.UtcNow;
        var order = MakeOrder(orderId, invoices: [older, newer]);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.EnsureEmailedAsync(orderId);

        _sender.Verify(s => s.SendAsync(newer, order, order.User, It.IsAny<CancellationToken>()),
            Times.Once);
        newer.EmailedAt.Should().NotBeNull();
        older.EmailedAt.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────
    //  Phase 6 — IssueCreditNoteAsync
    // ─────────────────────────────────────────────────────────

    private static Invoice MakePaidInvoice(Guid orderId,
        decimal amountHT = 100m,
        decimal vatAmount = 20m)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Date = DateTime.UtcNow.AddDays(-1),
            AmountHT = amountHT,
            VatAmount = vatAmount,
            AmountTTC = amountHT + vatAmount,
            Type = InvoiceType.Invoice,
            Status = InvoiceStatus.Paid,
        };

    [Fact]
    public async Task IssueCreditNoteAsync_HappyPath_CreatesCreditNote_WithProrataVat()
    {
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId, amountHT: 100m, vatAmount: 20m); // 20% effective
        var order = MakeOrder(orderId);
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var result = await _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(AmountHT: 50m, Reason: "Produit défectueux"));

        result.Type.Should().Be(InvoiceType.CreditNote);
        result.AmountHT.Should().Be(50m);
        // Prorata VAT at the original's effective rate (20m / 100m = 0.20).
        result.VatAmount.Should().Be(10m);
        result.AmountTTC.Should().Be(60m);
        result.RelatedInvoiceId.Should().Be(original.Id);
        result.Status.Should().Be(InvoiceStatus.Paid);
        _invoiceRepo.Verify(r => r.CreateAsync(It.Is<Invoice>(i =>
            i.Type == InvoiceType.CreditNote
            && i.RelatedInvoiceId == original.Id
            && i.OrderId == orderId)), Times.Once);
        _creditNoteSender.Verify(s => s.SendAsync(
            It.IsAny<Invoice>(),
            original,
            order,
            order.User,
            "Produit défectueux",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_UnknownInvoice_ThrowsNotFound()
    {
        _invoiceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Invoice?)null);

        var act = () => _sut.IssueCreditNoteAsync(Guid.NewGuid(),
            new IssueCreditNoteRequest(10m, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task IssueCreditNoteAsync_OnCreditNote_ThrowsNotAnInvoice()
    {
        // No credit-note-of-credit-note. Accounting wouldn't reconcile it.
        var orderId = Guid.NewGuid();
        var alreadyCredit = MakePaidInvoice(orderId);
        alreadyCredit.Type = InvoiceType.CreditNote;
        _invoiceRepo.Setup(r => r.GetByIdAsync(alreadyCredit.Id)).ReturnsAsync(alreadyCredit);

        var act = () => _sut.IssueCreditNoteAsync(alreadyCredit.Id,
            new IssueCreditNoteRequest(10m, null));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("not_an_invoice");
    }

    [Fact]
    public async Task IssueCreditNoteAsync_UnpaidInvoice_ThrowsNotPaid()
    {
        var orderId = Guid.NewGuid();
        var unpaid = MakePaidInvoice(orderId);
        unpaid.Status = InvoiceStatus.Pending;
        _invoiceRepo.Setup(r => r.GetByIdAsync(unpaid.Id)).ReturnsAsync(unpaid);

        var act = () => _sut.IssueCreditNoteAsync(unpaid.Id,
            new IssueCreditNoteRequest(10m, null));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("not_paid");
    }

    [Fact]
    public async Task IssueCreditNoteAsync_ZeroOrNegativeAmount_ThrowsInvalidAmount()
    {
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId);
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);

        var act = () => _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(0m, null));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("invalid_amount");
    }

    [Fact]
    public async Task IssueCreditNoteAsync_ExceedsOriginal_ThrowsExceedsRemaining()
    {
        // Original 100 € HT, request 150 € → rejected.
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId, amountHT: 100m, vatAmount: 20m);
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);

        var act = () => _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(AmountHT: 150m, Reason: null));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("exceeds_remaining");
        _invoiceRepo.Verify(r => r.CreateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_CumulativeExceedsOriginal_ThrowsExceedsRemaining()
    {
        // Partial credits OK as long as cumulative ≤ original.
        // Original 100 € HT, 60 € already credited via prior CN → only 40 €
        // remaining → a 50 € request must be rejected.
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId, amountHT: 100m, vatAmount: 20m);
        var priorCn = new Invoice
        {
            Id = Guid.NewGuid(), OrderId = orderId,
            Type = InvoiceType.CreditNote, RelatedInvoiceId = original.Id,
            AmountHT = 60m, VatAmount = 12m, AmountTTC = 72m,
        };
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _invoiceRepo.Setup(r => r.GetCreditNotesForInvoiceAsync(original.Id))
                    .ReturnsAsync(new[] { priorCn });

        var act = () => _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(AmountHT: 50m, Reason: null));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Reason.Should().Be("exceeds_remaining");
    }

    [Fact]
    public async Task IssueCreditNoteAsync_AtRemainingLimit_Succeeds()
    {
        // Boundary: original 100, 60 credited, request 40 → exactly fits.
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId, amountHT: 100m, vatAmount: 20m);
        var priorCn = new Invoice
        {
            Id = Guid.NewGuid(), OrderId = orderId,
            Type = InvoiceType.CreditNote, RelatedInvoiceId = original.Id,
            AmountHT = 60m, VatAmount = 12m, AmountTTC = 72m,
        };
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _invoiceRepo.Setup(r => r.GetCreditNotesForInvoiceAsync(original.Id))
                    .ReturnsAsync(new[] { priorCn });
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(MakeOrder(orderId));

        var result = await _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(AmountHT: 40m, Reason: "Solde"));

        result.AmountHT.Should().Be(40m);
        _invoiceRepo.Verify(r => r.CreateAsync(It.IsAny<Invoice>()), Times.Once);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_EmailSendFails_StillReturnsCreditNote()
    {
        // Best-effort: the credit note is in the DB. A flaky SMTP must not
        // unwind a financial document.
        var orderId = Guid.NewGuid();
        var original = MakePaidInvoice(orderId);
        var order = MakeOrder(orderId);
        _invoiceRepo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _creditNoteSender
            .Setup(s => s.SendAsync(It.IsAny<Invoice>(), original, order, order.User,
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));

        var result = await _sut.IssueCreditNoteAsync(original.Id,
            new IssueCreditNoteRequest(10m, "Refund"));

        result.Should().NotBeNull();
        result.Type.Should().Be(InvoiceType.CreditNote);
        _invoiceRepo.Verify(r => r.CreateAsync(It.IsAny<Invoice>()), Times.Once);
    }
}
