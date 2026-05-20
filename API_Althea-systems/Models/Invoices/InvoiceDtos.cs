namespace API_Althea_systems.Models.Invoices;

public record InvoiceDto(
    Guid Id,
    Guid OrderId,
    DateTime Date,
    decimal AmountHT,
    decimal VatAmount,
    decimal AmountTTC,
    InvoiceStatus Status,
    InvoiceType Type,
    Guid? RelatedInvoiceId
);

public record InvoiceCreateRequest(
    Guid OrderId,
    InvoiceType Type,
    Guid? RelatedInvoiceId
);

/// <summary>
/// Body of <c>POST /api/invoices/{id}/credit-note</c>.
/// <para><b>AmountHT</b>: pre-tax amount to credit, in EUR. Must be &gt; 0 and
///   ≤ the remaining creditable amount on the original invoice (original
///   total minus the sum of credit notes already issued against it).
///   The service auto-computes <c>VatAmount</c> at the original invoice's
///   effective VAT rate so the proportion stays consistent.</para>
/// <para><b>Reason</b>: free-text justification for the audit trail.
///   Persisted nowhere yet (kept in logs) — we can promote it to a column
///   if accounting needs it surfaced on the PDF.</para>
/// </summary>
public record IssueCreditNoteRequest(
    decimal AmountHT,
    string? Reason
);
