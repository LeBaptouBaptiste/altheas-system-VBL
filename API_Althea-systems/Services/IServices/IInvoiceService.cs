using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IInvoiceService
{
    Task<InvoiceDto> GetByIdAsync(Guid id);
    Task<PaginatedResponse<InvoiceDto>> GetAllAsync(int page, int pageSize);
    Task<InvoiceDto> CreateAsync(InvoiceCreateRequest request);
}
