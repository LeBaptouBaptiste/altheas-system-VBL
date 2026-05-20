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
}
