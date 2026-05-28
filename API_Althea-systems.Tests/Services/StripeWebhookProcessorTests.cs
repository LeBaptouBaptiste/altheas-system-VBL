using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.IServices;
using StripeApi = Stripe;

namespace API_Althea_systems.Tests.Services;

/// <summary>
/// Behaviour of the Stripe webhook dispatcher. The HTTP / signature
/// concerns live in StripeWebhookController; here we drive the processor
/// with synthetic Event objects and assert how the Order is mutated.
///
/// Critical scenarios:
///   • succeeded → Order Validated + Processing
///   • succeeded on already-Validated order → no-op (idempotency)
///   • succeeded on Cancelled order → does NOT resurrect (Status stays Cancelled)
///   • payment_failed → Order Failed
///   • payment_failed on already-Validated order → no-op
///   • stale event (intent id no longer matches order) → ignored
///   • metadata.orderId missing → ignored
///   • unknown event type → no-op, no exception
/// </summary>
public class StripeWebhookProcessorTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IInvoiceService> _invoices = new();
    private readonly StripeWebhookProcessor _sut;

    public StripeWebhookProcessorTests()
    {
        // Default the invoice ensure to a no-op so tests that don't care about
        // the invoice side-effect don't have to set it up explicitly.
        _invoices
            .Setup(s => s.EnsureForOrderAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new InvoiceDto(
                Guid.NewGuid(), Guid.NewGuid(), "INV-TEST-0001", DateTime.UtcNow,
                0m, 0m, 0m, InvoiceStatus.Paid, InvoiceType.Invoice, null));

        _sut = new StripeWebhookProcessor(
            _orders.Object,
            _users.Object,
            _invoices.Object,
            NullLogger<StripeWebhookProcessor>.Instance);
    }

    private static StripeApi.Event MakeEvent(string type, StripeApi.PaymentIntent intent) => new()
    {
        Id = $"evt_{Guid.NewGuid():N}",
        Type = type,
        Data = new StripeApi.EventData { Object = intent },
    };

    private static StripeApi.PaymentIntent MakeIntent(string id, Guid orderId, string status = "succeeded") => new()
    {
        Id = id,
        Status = status,
        Metadata = new Dictionary<string, string> { ["orderId"] = orderId.ToString() },
    };

    private static Order MakeOrder(
        Guid id,
        string intentId,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        OrderStatus status = OrderStatus.Pending)
        => new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Status = status,
            PaymentStatus = paymentStatus,
            PaymentMethod = PaymentMethod.Card,
            ShippingMethod = ShippingMethod.Standard,
            BillingAddressId = Guid.NewGuid(),
            ShippingAddressId = Guid.NewGuid(),
            StripePaymentIntentId = intentId,
        };

    // ── succeeded ─────────────────────────────────────────

    [Fact]
    public async Task Succeeded_PendingOrder_MarksValidatedAndProcessing()
    {
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_1", orderId);
        var order = MakeOrder(orderId, "pi_1");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        order.PaymentStatus.Should().Be(PaymentStatus.Validated);
        order.Status.Should().Be(OrderStatus.Processing);
        order.StripePaymentStatus.Should().Be("succeeded");
        _orders.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task Succeeded_PendingOrder_AutoIssuesInvoice()
    {
        // The whole point of this hook for the customer is "I just paid →
        // let me download my invoice". Verify EnsureForOrderAsync gets fired
        // for the order we just validated.
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_inv", orderId);
        var order = MakeOrder(orderId, "pi_inv");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        _invoices.Verify(s => s.EnsureForOrderAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task Succeeded_PendingOrder_TriggersOrderConfirmationEmail()
    {
        // Phase 3: after issuing the invoice, the webhook must call
        // EnsureEmailedAsync so the customer gets the receipt + PDF.
        // Idempotency is the SUT's responsibility (Invoice.EmailedAt) —
        // here we just verify the call happens.
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_mail", orderId);
        var order = MakeOrder(orderId, "pi_mail");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        _invoices.Verify(s => s.EnsureEmailedAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Succeeded_EmailSendingFails_DoesNotThrow()
    {
        // Same defensive contract as the invoice-issuance path: a flaky SMTP
        // must NOT cause Stripe to retry the payment validation. Webhook
        // returns success; the email retry is the SUT's problem (EmailedAt
        // stays null so a later trigger can attempt again).
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_mailfail", orderId);
        var order = MakeOrder(orderId, "pi_mailfail");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _invoices
            .Setup(s => s.EnsureEmailedAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var act = () => _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        await act.Should().NotThrowAsync();
        order.PaymentStatus.Should().Be(PaymentStatus.Validated);
    }

    [Fact]
    public async Task Succeeded_InvoiceIssuanceFails_DoesNotThrow()
    {
        // The payment is valid even if invoice generation blows up — we log
        // and move on; the startup backfill will retry. Surfacing the error
        // would mean Stripe retries the webhook forever for a problem the
        // customer can't fix.
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_boom", orderId);
        var order = MakeOrder(orderId, "pi_boom");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _invoices
            .Setup(s => s.EnsureForOrderAsync(orderId))
            .ThrowsAsync(new InvalidOperationException("pdf service down"));

        var act = () => _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        await act.Should().NotThrowAsync();
        order.PaymentStatus.Should().Be(PaymentStatus.Validated);
    }

    [Fact]
    public async Task Succeeded_AlreadyValidated_IsNoOp()
    {
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_2", orderId);
        var order = MakeOrder(orderId, "pi_2", PaymentStatus.Validated, OrderStatus.Processing);
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Succeeded_OnCancelledOrder_DoesNotResurrectStatus()
    {
        // Admin cancelled while Stripe was confirming; we accept the payment
        // (PaymentStatus = Validated) but the order stays Cancelled — the
        // billing reconciliation / refund is a separate workflow.
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_3", orderId);
        var order = MakeOrder(orderId, "pi_3", PaymentStatus.Pending, OrderStatus.Cancelled);
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        order.PaymentStatus.Should().Be(PaymentStatus.Validated);
        order.Status.Should().Be(OrderStatus.Cancelled, "we never roll back an admin Cancelled");
    }

    [Fact]
    public async Task Succeeded_StaleIntentId_IsIgnored()
    {
        // User retried payment ⇒ a new PaymentIntent was created and the
        // order now points to a different id. The old event is obsolete.
        var orderId = Guid.NewGuid();
        var staleIntent = MakeIntent("pi_old", orderId);
        var order = MakeOrder(orderId, "pi_new_current");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", staleIntent));

        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Succeeded_MissingOrderIdMetadata_IsIgnored()
    {
        var intent = new StripeApi.PaymentIntent
        {
            Id = "pi_no_meta",
            Status = "succeeded",
            Metadata = new Dictionary<string, string>(),
        };

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        _orders.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Succeeded_UnknownOrder_IsIgnored()
    {
        // Could happen if the webhook URL is shared across environments
        // (e.g. someone in another team's local dev forwarding to prod).
        var intent = MakeIntent("pi_orphan", Guid.NewGuid());
        _orders.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        await _sut.ProcessAsync(MakeEvent("payment_intent.succeeded", intent));

        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    // ── payment_failed ───────────────────────────────────

    [Fact]
    public async Task PaymentFailed_PendingOrder_MarksFailed()
    {
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_f", orderId, status: "requires_payment_method");
        var order = MakeOrder(orderId, "pi_f");
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.payment_failed", intent));

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        order.StripePaymentStatus.Should().Be("requires_payment_method");
        _orders.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task PaymentFailed_AlreadyValidated_IsNoOp()
    {
        // Stripe can send a stale failed event during retries after the
        // user successfully paid on a later attempt — never roll back.
        var orderId = Guid.NewGuid();
        var intent = MakeIntent("pi_g", orderId, status: "requires_payment_method");
        var order = MakeOrder(orderId, "pi_g", PaymentStatus.Validated, OrderStatus.Processing);
        _orders.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _sut.ProcessAsync(MakeEvent("payment_intent.payment_failed", intent));

        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    // ── payment_method.attached ──────────────────────────

    private static StripeApi.PaymentMethod MakeCard(string id, string customerId, string brand = "visa", string last4 = "4242") => new()
    {
        Id = id,
        CustomerId = customerId,
        Type = "card",
        Card = new StripeApi.PaymentMethodCard { Brand = brand, Last4 = last4, ExpMonth = 12, ExpYear = 2030 },
    };

    private static StripeApi.Event MakeMethodEvent(string type, StripeApi.PaymentMethod method) => new()
    {
        Id = $"evt_{Guid.NewGuid():N}",
        Type = type,
        Data = new StripeApi.EventData { Object = method },
    };

    private static User MakeUserWithCustomer(string customerId, params UserPaymentMethod[] existingMethods) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        Email = "test@test.com",
        PasswordHash = "x",
        Role = UserRole.Customer,
        Status = UserStatus.Active,
        StripeCustomerId = customerId,
        PaymentMethods = existingMethods.ToList(),
    };

    [Fact]
    public async Task PaymentMethodAttached_NewCard_PersistsOnUser()
    {
        var customerId = "cus_abc";
        var user = MakeUserWithCustomer(customerId);
        var pm = MakeCard("pm_123", customerId, brand: "visa", last4: "4242");
        _users.Setup(r => r.GetByStripeCustomerIdAsync(customerId)).ReturnsAsync(user);

        await _sut.ProcessAsync(MakeMethodEvent("payment_method.attached", pm));

        user.PaymentMethods.Should().HaveCount(1);
        var saved = user.PaymentMethods.Single();
        saved.StripePaymentMethodId.Should().Be("pm_123");
        saved.Brand.Should().Be("visa");
        saved.Last4.Should().Be("4242");
        saved.ExpMonth.Should().Be(12);
        saved.ExpYear.Should().Be(2030);
        saved.Label.Should().Be("Visa •••• 4242", "the label should be human-friendly");
        _users.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task PaymentMethodAttached_DuplicateAttach_IsNoOp()
    {
        // Stripe redelivers, or the user attaches the same card twice.
        var customerId = "cus_dup";
        var existing = new UserPaymentMethod
        {
            Id = Guid.NewGuid(), Type = "card", Label = "Visa •••• 4242",
            StripePaymentMethodId = "pm_dup", Brand = "visa", Last4 = "4242",
        };
        var user = MakeUserWithCustomer(customerId, existing);
        var pm = MakeCard("pm_dup", customerId);
        _users.Setup(r => r.GetByStripeCustomerIdAsync(customerId)).ReturnsAsync(user);

        await _sut.ProcessAsync(MakeMethodEvent("payment_method.attached", pm));

        user.PaymentMethods.Should().HaveCount(1);
        _users.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task PaymentMethodAttached_NonCardType_IsIgnored()
    {
        // SEPA, link, paypal etc. — out of scope for now.
        var pm = new StripeApi.PaymentMethod { Id = "pm_sepa", CustomerId = "cus_x", Type = "sepa_debit" };

        await _sut.ProcessAsync(MakeMethodEvent("payment_method.attached", pm));

        _users.Verify(r => r.GetByStripeCustomerIdAsync(It.IsAny<string>()), Times.Never);
        _users.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task PaymentMethodAttached_NoCustomer_IsIgnored()
    {
        // Defensive: a PaymentMethod with no customer is orphaned and shouldn't
        // happen with our flow, but Stripe doesn't prevent it.
        var pm = new StripeApi.PaymentMethod { Id = "pm_orphan", Type = "card", Card = new StripeApi.PaymentMethodCard { Brand = "visa", Last4 = "0000" } };

        await _sut.ProcessAsync(MakeMethodEvent("payment_method.attached", pm));

        _users.Verify(r => r.GetByStripeCustomerIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PaymentMethodAttached_UnknownCustomer_IsIgnored()
    {
        // Env mismatch: webhook from a different Stripe account got routed here.
        var pm = MakeCard("pm_x", "cus_unknown");
        _users.Setup(r => r.GetByStripeCustomerIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await _sut.ProcessAsync(MakeMethodEvent("payment_method.attached", pm));

        _users.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // ── unknown event ────────────────────────────────────

    [Fact]
    public async Task UnknownEventType_IsNoOp()
    {
        var intent = MakeIntent("pi_z", Guid.NewGuid());
        var act = () => _sut.ProcessAsync(MakeEvent("charge.refunded", intent));

        await act.Should().NotThrowAsync();
        _orders.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }
}
