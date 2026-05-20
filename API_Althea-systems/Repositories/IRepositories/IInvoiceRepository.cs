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
}
