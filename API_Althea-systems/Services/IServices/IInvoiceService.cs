using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IInvoiceService
{
    Task<InvoiceDto> GetByIdAsync(Guid id);
    Task<PaginatedResponse<InvoiceDto>> GetAllAsync(int page, int pageSize);
    Task<InvoiceDto> CreateAsync(InvoiceCreateRequest request);

    /// <summary>
    /// Idempotent: returns the existing Type=Invoice attached to the order
    /// if there is one, otherwise creates a fresh Paid invoice from the
    /// order totals. Called from the Stripe webhook on payment_intent.succeeded
    /// so a valid invoice is available the moment the buyer's payment clears.
    /// Safe to call multiple times — Stripe redelivers and we run a startup
    /// backfill that may hit the same order.
    /// </summary>
    Task<InvoiceDto> EnsureForOrderAsync(Guid orderId);

    /// <summary>
    /// Idempotent: if the order's invoice has not been emailed yet
    /// (<see cref="Invoice.EmailedAt"/> is null), renders the PDF and sends
    /// the "order confirmation" email with it attached, then marks EmailedAt.
    /// Safe to call multiple times — Stripe webhook redelivery, admin
    /// re-trigger, etc. No-op if the order has no Type=Invoice yet (caller
    /// should call <see cref="EnsureForOrderAsync"/> first).
    /// </summary>
    Task EnsureEmailedAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Phase 6: issues a credit note (<see cref="Models.Invoices.InvoiceType.CreditNote"/>)
    /// against an existing paid invoice. Enforced invariants:
    /// <list type="bullet">
    ///   <item>Original invoice must exist and be <see cref="Models.Invoices.InvoiceType.Invoice"/>
    ///         (no credit-notes-of-credit-notes).</item>
    ///   <item>Original must be <see cref="Models.Invoices.InvoiceStatus.Paid"/>
    ///         (refunding an unpaid invoice makes no sense — just cancel it).</item>
    ///   <item>Amount must be &gt; 0 and ≤ original.AmountHT − sum of prior
    ///         credit notes against the same invoice. Supports partial /
    ///         multiple credits as long as the cumulative total stays ≤
    ///         the original.</item>
    /// </list>
    /// VAT is auto-prorated at the original invoice's effective rate.
    /// Fires <c>ICreditNoteSender</c> (best-effort) to mail the PDF.
    /// </summary>
    Task<InvoiceDto> IssueCreditNoteAsync(
        Guid originalInvoiceId,
        IssueCreditNoteRequest request,
        CancellationToken ct = default);
}
