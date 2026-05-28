namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Phase 7: thin wrapper around Stripe's RefundService for the credit-note
/// Refund mode. Lives separately from <see cref="IStripeService"/> so
/// InvoiceService can mock just the refund surface without dragging in
/// PaymentIntent / Customer / PaymentMethod plumbing in tests.
/// </summary>
public interface IStripeRefundService
{
    /// <summary>
    /// Issues a refund on the given PaymentIntent.
    /// </summary>
    /// <param name="paymentIntentId">pi_xxx of the original successful payment.</param>
    /// <param name="amountCents">Amount to refund, in the smallest currency unit
    ///   (cents for EUR). Must not exceed the PaymentIntent's captured total
    ///   minus prior refunds — Stripe rejects otherwise.</param>
    /// <param name="reason">Optional Stripe-side reason ("duplicate" /
    ///   "fraudulent" / "requested_by_customer"). Most credit notes are
    ///   "requested_by_customer" — pass null to omit.</param>
    /// <returns>(refundId, status) — status is Stripe's verbatim string
    ///   ("succeeded" / "pending" / "failed").</returns>
    Task<(string RefundId, string Status)> RefundAsync(
        string paymentIntentId,
        long amountCents,
        string? reason = null,
        CancellationToken ct = default);
}
