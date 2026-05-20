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
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        _sut = new InvoiceService(
            _invoiceRepo.Object,
            _orderRepo.Object,
            _sender.Object,
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
}
