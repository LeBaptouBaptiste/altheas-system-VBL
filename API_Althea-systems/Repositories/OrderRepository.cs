using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AltheaDbContext _context;

    public OrderRepository(AltheaDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.BillingAddress)
            .Include(o => o.ShippingAddress)
            .Include(o => o.StatusHistory)
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(int page, int pageSize, Guid? userId = null)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.BillingAddress)
            .Include(o => o.ShippingAddress)
            .Include(o => o.StatusHistory)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(Guid? userId = null)
    {
        var query = _context.Orders.AsQueryable();
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
        return await query.CountAsync();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
}
