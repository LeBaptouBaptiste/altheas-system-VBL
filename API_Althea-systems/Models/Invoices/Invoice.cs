using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Invoices;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    /// <summary>
    /// Human-readable invoice number in the format
    /// <c>{ClientCode}-{YYYY}-{MM}-{NNNN}</c> where:
    /// <list type="bullet">
    ///   <item>ClientCode = first 8 hex chars of the customer's User.Id (stable, anonymous-looking)</item>
    ///   <item>YYYY-MM = year and month of issue</item>
    ///   <item>NNNN = monthly sequence per customer, zero-padded to 4 digits</item>
    /// </list>
    /// Generated once at invoice creation (<see cref="API_Althea_systems.Services.InvoiceService"/>);
    /// immutable thereafter — matches the French accounting requirement that
    /// invoice numbers can't be changed after issue. Credit notes follow the
    /// same format (the Type field distinguishes them).
    /// </summary>
    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public decimal AmountHT { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountTTC { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public InvoiceType Type { get; set; } = InvoiceType.Invoice;
    public Guid? RelatedInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Phase 7 (credit-note money movement) ─────────────

    /// <summary>
    /// Set only when <see cref="Type"/> is CreditNote — how the refund
    /// was actually settled. Null on regular invoices.
    /// </summary>
    public CreditNoteMode? Mode { get; set; }

    /// <summary>
    /// Stripe refund id (re_xxx) when <see cref="Mode"/> is Refund.
    /// Persisted so we can correlate with the Stripe Dashboard and ignore
    /// duplicate <c>charge.refunded</c> webhook deliveries.
    /// </summary>
    public string? StripeRefundId { get; set; }

    /// <summary>
    /// Raw Stripe refund status verbatim ("succeeded" / "pending" / "failed" /
    /// "canceled"). Updated by the webhook handler. Null until a refund is
    /// actually issued. For Mode=StoreCredit this stays null.
    /// </summary>
    public string? RefundStatus { get; set; }

    /// <summary>
    /// UTC moment the order-confirmation email + PDF attachment was sent to
    /// the customer. Null until then. Used by InvoiceService.EnsureEmailedAsync
    /// as the idempotency key: Stripe webhook redelivery / startup retry /
    /// admin re-trigger all skip if non-null. Phase 3 (email).
    /// </summary>
    public DateTime? EmailedAt { get; set; }

    // Navigation
    public Order.Order Order { get; set; } = null!;
    public Invoice? RelatedInvoice { get; set; }
}

public enum InvoiceStatus
{
    Paid,
    Pending,
    Overdue,
    Cancelled
}

public enum InvoiceType
{
    Invoice,
    CreditNote
}
