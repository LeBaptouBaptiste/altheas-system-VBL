using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
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
    private readonly StripeWebhookProcessor _sut;

    public StripeWebhookProcessorTests()
    {
        _sut = new StripeWebhookProcessor(_orders.Object, NullLogger<StripeWebhookProcessor>.Instance);
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
