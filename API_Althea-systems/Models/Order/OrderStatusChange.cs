using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Order;

public class OrderStatusChange
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderStatus From { get; set; }
    public OrderStatus To { get; set; }
    public DateTime Date { get; set; }
    public Guid UserId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Users.User ChangedBy { get; set; } = null!;
}
