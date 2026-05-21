using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Invoices;

public record InvoiceDto(
    Guid Id,
    Guid OrderId,
    // Human-readable invoice number ({ClientCode}-{YYYY}-{MM}-{NNNN}).
    // This is what's displayed everywhere — admin tables, account history,
    // PDF headers, email subject lines. Distinct from the internal Guid Id.
    string Number,
    DateTime Date,
    decimal AmountHT,
    decimal VatAmount,
    decimal AmountTTC,
    InvoiceStatus Status,
    InvoiceType Type,
    Guid? RelatedInvoiceId,
    // Phase 7: populated only when Type=CreditNote.
    CreditNoteMode? Mode = null,
    string? StripeRefundId = null,
    string? RefundStatus = null
);

public record InvoiceCreateRequest(
    Guid OrderId,
    InvoiceType Type,
    Guid? RelatedInvoiceId
);

/// <summary>
/// Body of <c>POST /api/invoices/{id}/credit-note</c>.
/// <para><b>AmountHT</b>: pre-tax amount to credit, in EUR. Must be &gt; 0 and
///   ≤ the remaining creditable amount on the original invoice.</para>
/// <para><b>Mode</b>: how the credit moves — <c>Refund</c> sends money back to
///   the customer's card via Stripe; <c>StoreCredit</c> adds it to their
///   in-app balance for use at the next checkout.</para>
/// <para><b>Reason</b>: free-text justification (≤ 500 chars).</para>
/// </summary>
public record IssueCreditNoteRequest(
    decimal AmountHT,
    CreditNoteMode Mode,
    string? Reason
);
