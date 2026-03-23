using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderDto> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);
        return MapToDto(order);
    }

    public async Task<PaginatedResponse<OrderDto>> GetAllAsync(int page, int pageSize, Guid? userId = null)
    {
        var orders = await _orderRepository.GetAllAsync(page, pageSize, userId);
        var total = await _orderRepository.CountAsync(userId);
        return new PaginatedResponse<OrderDto>(
            orders.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<OrderDto> CreateAsync(Guid userId, OrderCreateRequest request)
    {
        var items = new List<OrderItem>();
        foreach (var itemReq in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemReq.ProductId)
                ?? throw new NotFoundException("Product", itemReq.ProductId);

            items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductNameFr = product.NameFr,
                ProductNameEn = product.NameEn,
                Quantity = itemReq.Quantity,
                PriceHT = product.PriceHT,
                VatRate = product.VatRate
            });
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = DateTime.UtcNow,
            BillingAddressId = request.BillingAddressId,
            ShippingAddressId = request.ShippingAddressId,
            ShippingMethod = request.ShippingMethod,
            PaymentMethod = request.PaymentMethod,
            ShippingCost = GetShippingCost(request.ShippingMethod),
            Items = items
        };

        await _orderRepository.CreateAsync(order);
        return await GetByIdAsync(order.Id);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, Guid changedByUserId, OrderStatusUpdateRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);

        var statusChange = new OrderStatusChange
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            From = order.Status,
            To = request.Status,
            Date = DateTime.UtcNow,
            UserId = changedByUserId
        };

        order.StatusHistory.Add(statusChange);
        order.Status = request.Status;

        await _orderRepository.UpdateAsync(order);
        return await GetByIdAsync(order.Id);
    }

    private static decimal GetShippingCost(Common.Enums.ShippingMethod method) => method switch
    {
        Common.Enums.ShippingMethod.Standard => 15m,
        Common.Enums.ShippingMethod.Express => 35m,
        Common.Enums.ShippingMethod.Overnight => 75m,
        _ => 15m
    };

    private static OrderDto MapToDto(Order o)
    {
        var totalHT = o.Items.Sum(i => i.PriceHT * i.Quantity);
        var totalVAT = o.Items.Sum(i => i.PriceHT * i.Quantity * GetVatMultiplier(i.VatRate));
        return new OrderDto(
            o.Id, o.UserId, o.User?.Name ?? "", o.Date, o.Status, o.PaymentStatus,
            o.PaymentMethod,
            MapAddress(o.BillingAddress), MapAddress(o.ShippingAddress),
            o.ShippingMethod, o.ShippingCost, totalHT, totalVAT, totalHT + totalVAT + o.ShippingCost,
            o.CreatedAt, o.UpdatedAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductNameFr, i.ProductNameEn, i.Quantity, i.PriceHT, i.VatRate)),
            o.StatusHistory.Select(s => new OrderStatusChangeDto(s.From, s.To, s.Date, s.UserId))
        );
    }

    private static decimal GetVatMultiplier(Common.Enums.VatRate rate) => rate switch
    {
        Common.Enums.VatRate.Standard => 0.20m,
        Common.Enums.VatRate.Intermediate => 0.10m,
        Common.Enums.VatRate.Reduced => 0.055m,
        _ => 0m
    };

    private static AddressDto MapAddress(Models.Users.Address? a) =>
        a == null ? new AddressDto(Guid.Empty, "", "", "", null, "", null, "", "", "", null)
        : new AddressDto(a.Id, a.Label, a.FirstName, a.LastName, a.Company, a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone);
}
