using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;
using Stripe;

namespace API_Althea_systems.Services;

public class StripeWebhookProcessor : IStripeWebhookProcessor
{
    private readonly IOrderRepository _orders;
    private readonly IUserRepository _users;
    private readonly ILogger<StripeWebhookProcessor> _logger;

    public StripeWebhookProcessor(
        IOrderRepository orders,
        IUserRepository users,
        ILogger<StripeWebhookProcessor> logger)
    {
        _orders = orders;
        _users = users;
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
            case "payment_method.attached":
                await HandlePaymentMethodAttached(stripeEvent, ct);
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

    private async Task HandlePaymentMethodAttached(Event stripeEvent, CancellationToken ct)
    {
        // Fired when a PaymentMethod gets attached to a Customer — which is what
        // setup_future_usage='off_session' triggers under the hood when the user
        // confirms a payment with "save card" checked. The event is the only
        // reliable place to persist the saved card, because the front-end
        // confirmation can be interrupted (closed tab, crash, …) but Stripe
        // will still send this webhook.
        var method = stripeEvent.Data.Object as Stripe.PaymentMethod;
        if (method is null)
        {
            _logger.LogWarning("payment_method.attached {EventId} had no PaymentMethod payload.", stripeEvent.Id);
            return;
        }

        // We only persist cards — other types (sepa_debit, link, paypal…) need
        // their own UI and metadata schema. Logging keeps the noise visible.
        if (method.Type != "card" || method.Card is null)
        {
            _logger.LogInformation(
                "payment_method.attached {MethodId} is type '{Type}', not a card — skipping.",
                method.Id, method.Type);
            return;
        }

        if (string.IsNullOrEmpty(method.CustomerId))
        {
            _logger.LogWarning("PaymentMethod {MethodId} has no customer — orphaned event, skipping.", method.Id);
            return;
        }

        var user = await _users.GetByStripeCustomerIdAsync(method.CustomerId);
        if (user is null)
        {
            _logger.LogWarning(
                "payment_method.attached {MethodId} for unknown customer {CustomerId} — env mismatch or stale Stripe data.",
                method.Id, method.CustomerId);
            return;
        }

        // Idempotency: Stripe can redeliver. Same pm_id for the same user → no-op.
        if (user.PaymentMethods.Any(p => p.StripePaymentMethodId == method.Id))
        {
            _logger.LogInformation(
                "PaymentMethod {MethodId} already saved for user {UserId}, skipping.",
                method.Id, user.Id);
            return;
        }

        var brand = method.Card.Brand ?? "card";
        var last4 = method.Card.Last4 ?? "????";

        user.PaymentMethods.Add(new UserPaymentMethod
        {
            // Leave Id at default(Guid): adding via tracked parent's
            // navigation collection — EF generates a fresh PK and marks
            // the entity as Added. Setting Id explicitly would trigger
            // EF's "this exists" heuristic and emit UPDATE → 0 rows.
            UserId = user.Id,
            Type = "card",
            // Display label as it'll appear in /account/payments and at checkout.
            // Capitalize Brand for a clean "Visa •••• 4242" rather than "visa".
            Label = $"{Capitalize(brand)} •••• {last4}",
            StripePaymentMethodId = method.Id,
            Brand = brand,
            Last4 = last4,
            ExpMonth = (int?)method.Card.ExpMonth,
            ExpYear = (int?)method.Card.ExpYear,
            CreatedAt = DateTime.UtcNow,
        });
        await _users.UpdateAsync(user);

        _logger.LogInformation(
            "Saved PaymentMethod {MethodId} ({Brand} •••• {Last4}) for user {UserId}.",
            method.Id, brand, last4, user.Id);
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

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
