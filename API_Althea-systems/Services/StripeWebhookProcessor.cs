using API_Althea_systems.Common.Enums;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;
using Stripe;

namespace API_Althea_systems.Services;

public class StripeWebhookProcessor : IStripeWebhookProcessor
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<StripeWebhookProcessor> _logger;

    public StripeWebhookProcessor(IOrderRepository orders, ILogger<StripeWebhookProcessor> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task ProcessAsync(Event stripeEvent, CancellationToken ct = default)
    {
        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await HandlePaymentSucceeded(stripeEvent, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentFailed(stripeEvent, ct);
                break;
            default:
                // Stripe sends a lot of events we don't care about (charge.*,
                // customer.*, etc.). Log at info so ops can audit without
                // alerting on every noisy delivery.
                _logger.LogInformation(
                    "Stripe webhook {Type} ({EventId}) ignored: no handler registered.",
                    stripeEvent.Type, stripeEvent.Id);
                break;
        }
    }

    private async Task HandlePaymentSucceeded(Event stripeEvent, CancellationToken ct)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent is null)
        {
            _logger.LogWarning("payment_intent.succeeded event {EventId} had no PaymentIntent payload.", stripeEvent.Id);
            return;
        }

        // Match the order via the PaymentIntent id we persisted at creation
        // (see PaymentIntentController). The metadata also carries orderId
        // as a sanity-check belt-and-braces value.
        var order = await FindOrderByIntent(intent);
        if (order is null)
        {
            _logger.LogWarning(
                "payment_intent.succeeded {IntentId} did not match any local order — webhook from a different environment?",
                intent.Id);
            return;
        }

        // Idempotent: if the order was already validated, do nothing.
        // Stripe can redeliver; tolerate it.
        if (order.PaymentStatus == PaymentStatus.Validated)
        {
            _logger.LogInformation(
                "Order {OrderId} already validated, skipping redundant succeeded event.",
                order.Id);
            return;
        }

        order.PaymentStatus = PaymentStatus.Validated;
        order.StripePaymentStatus = intent.Status;
        // Move workflow forward: Pending → Processing. If the order has
        // been Cancelled in the meantime (admin action), don't resurrect it.
        if (order.Status == OrderStatus.Pending)
            order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;

        await _orders.UpdateAsync(order);

        _logger.LogInformation(
            "Order {OrderId} marked as Validated via PaymentIntent {IntentId}.",
            order.Id, intent.Id);
    }

    private async Task HandlePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent is null)
        {
            _logger.LogWarning("payment_intent.payment_failed event {EventId} had no PaymentIntent payload.", stripeEvent.Id);
            return;
        }

        var order = await FindOrderByIntent(intent);
        if (order is null)
        {
            _logger.LogWarning(
                "payment_intent.payment_failed {IntentId} did not match any local order.",
                intent.Id);
            return;
        }

        // Don't overwrite a Validated payment — Stripe can send a stale
        // failed event during retries; the user may have successfully paid
        // on a later attempt.
        if (order.PaymentStatus == PaymentStatus.Validated)
        {
            _logger.LogInformation(
                "Ignoring payment_failed for already-validated order {OrderId}.",
                order.Id);
            return;
        }

        order.PaymentStatus = PaymentStatus.Failed;
        order.StripePaymentStatus = intent.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _orders.UpdateAsync(order);

        _logger.LogInformation(
            "Order {OrderId} marked as Failed via PaymentIntent {IntentId} (status={IntentStatus}).",
            order.Id, intent.Id, intent.Status);
    }

    /// <summary>
    /// Looks up the order using the PaymentIntent. The id is the primary
    /// match; metadata.orderId is checked too as a defense against a Stripe
    /// account misconfiguration that could route events to the wrong tenant.
    /// </summary>
    private async Task<Models.Order.Order?> FindOrderByIntent(PaymentIntent intent)
    {
        // We can't filter by StripePaymentIntentId via a repo method that
        // doesn't exist yet, so we lean on metadata.orderId which we set
        // at intent creation. Two sources of truth → if they disagree, we
        // refuse to act (defensive).
        if (!intent.Metadata.TryGetValue("orderId", out var orderIdRaw)
            || !Guid.TryParse(orderIdRaw, out var orderId))
        {
            return null;
        }

        var order = await _orders.GetByIdAsync(orderId);
        if (order is null) return null;

        if (order.StripePaymentIntentId != intent.Id)
        {
            // Intent id on the order changed (user retried payment, new
            // PaymentIntent created) — this event is for an obsolete intent.
            _logger.LogWarning(
                "PaymentIntent {IntentId} from event does not match order {OrderId}'s current intent {CurrentIntentId} — ignoring stale event.",
                intent.Id, order.Id, order.StripePaymentIntentId);
            return null;
        }

        return order;
    }
}
