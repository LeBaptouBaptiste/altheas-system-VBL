using Stripe;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Dispatches verified Stripe webhook events to per-type handlers.
/// Lives behind the controller so the HTTP concerns (raw-body, signature)
/// stay separate from the business logic (looking up orders, updating
/// status), and so the dispatch table can be unit-tested without spinning
/// up an ASP.NET test host.
///
/// Handlers MUST be idempotent: a successful event can be re-sent by Stripe
/// (retry, manual replay) and the table-level idempotency in the controller
/// is not a hard guarantee under concurrent delivery.
/// </summary>
public interface IStripeWebhookProcessor
{
    Task ProcessAsync(Event stripeEvent, CancellationToken ct = default);
}
