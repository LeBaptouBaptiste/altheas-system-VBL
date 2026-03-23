using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Order;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public Guid BillingAddressId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
    public decimal ShippingCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Users.User User { get; set; } = null!;
    public Users.Address BillingAddress { get; set; } = null!;
    public Users.Address ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusChange> StatusHistory { get; set; } = [];
    public ICollection<Invoices.Invoice> Invoices { get; set; } = [];
}
