using API_Althea_systems.Models.Invoices;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<Invoice>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Invoice>> GetByOrderIdAsync(Guid orderId);
    Task<int> CountAsync();
    Task<Invoice> CreateAsync(Invoice invoice);
}
