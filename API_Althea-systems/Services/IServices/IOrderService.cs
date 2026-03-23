using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(Guid id);
    Task<PaginatedResponse<OrderDto>> GetAllAsync(int page, int pageSize, Guid? userId = null);
    Task<OrderDto> CreateAsync(Guid userId, OrderCreateRequest request);
    Task<OrderDto> UpdateStatusAsync(Guid id, Guid changedByUserId, OrderStatusUpdateRequest request);
}
