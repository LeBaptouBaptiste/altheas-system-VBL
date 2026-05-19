namespace API_Althea_systems.Models.Payments;

/// <summary>
/// Idempotency record for inbound Stripe webhook events. Inserted at the
/// start of the webhook handler — if the insert fails on the unique
/// EventId, the event has already been processed and the handler returns
/// 200 OK without re-running side effects.
///
/// Stripe redelivers webhooks on transient HTTP errors and supports manual
/// replay from the dashboard, so duplicates are expected.
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// Stripe event ID — globally unique, prefixed "evt_". Used as the
    /// primary key so the unique constraint enforces idempotency.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Event type, e.g. "payment_intent.succeeded" — kept for debugging
    /// and so dashboards can filter without reparsing the payload.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Server-side timestamp when we accepted the event. Distinct from
    /// the Stripe-side "created" timestamp (which we don't persist —
    /// it's available in the raw event payload if needed).
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
