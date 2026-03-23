using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AltheaDbContext _context;

    public InvoiceRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _context.Invoices
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Invoices
            .Include(i => i.Order)
            .OrderByDescending(i => i.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Invoices
            .Where(i => i.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Invoices.CountAsync();
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }
}
