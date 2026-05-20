using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

/// <summary>
/// Phase 7: how a credit note's money actually moves.
/// Set on <see cref="Models.Invoices.Invoice.Mode"/> ONLY when
/// <see cref="Models.Invoices.Invoice.Type"/> is <c>CreditNote</c>;
/// null on regular invoices.
///
/// Order MUST match front/src/lib/enums.ts.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CreditNoteMode
{
    /// <summary>
    /// 0 — Stripe refund triggered on the original PaymentIntent. Money
    /// is sent back to the customer's card (3-5 business days). The
    /// <see cref="Models.Invoices.Invoice.StripeRefundId"/> is populated
    /// at issuance.
    /// </summary>
    Refund,

    /// <summary>
    /// 1 — credit added to <see cref="Models.Users.User.CreditBalanceCents"/>.
    /// The customer can apply it at the next checkout via
    /// <c>OrderCreateRequest.CreditAppliedCents</c>.
    /// </summary>
    StoreCredit,
}
