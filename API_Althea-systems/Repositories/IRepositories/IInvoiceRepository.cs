using API_Althea_systems.Models.Invoices;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<Invoice>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Invoice>> GetByOrderIdAsync(Guid orderId);
    Task<int> CountAsync();
    Task<Invoice> CreateAsync(Invoice invoice);

    /// <summary>
    /// Persists changes to an already-tracked or detached Invoice. The hot
    /// path is "mark EmailedAt after the confirmation mail goes out" — we
    /// don't have a focused PATCH because invoices are immutable except
    /// for that one field and Status (admin-driven flows).
    /// </summary>
    Task UpdateAsync(Invoice invoice);

    /// <summary>
    /// Phase 6: returns all credit notes (<see cref="InvoiceType.CreditNote"/>)
    /// linked to the given original invoice via RelatedInvoiceId. Used by
    /// InvoiceService.IssueCreditNoteAsync to compute the remaining creditable
    /// amount (original total minus sum of existing credit notes).
    /// </summary>
    Task<IEnumerable<Invoice>> GetCreditNotesForInvoiceAsync(Guid originalInvoiceId);

    /// <summary>
    /// Count invoices (both regular invoices AND credit notes — they share
    /// the same numbering sequence per VBL spec) issued for a given customer
    /// within a [start, end) UTC range. Used by InvoiceService to compute the
    /// next monthly sequence number when generating the human-readable
    /// invoice Number.
    /// </summary>
    Task<int> CountByUserAndPeriodAsync(Guid userId, DateTime startInclusive, DateTime endExclusive);
}
