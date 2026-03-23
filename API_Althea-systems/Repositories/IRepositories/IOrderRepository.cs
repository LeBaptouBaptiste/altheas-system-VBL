using API_Althea_systems.Models.Order;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetAllAsync(int page, int pageSize, Guid? userId = null);
    Task<int> CountAsync(Guid? userId = null);
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
}
