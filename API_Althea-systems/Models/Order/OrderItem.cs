using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Order;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameFr { get; set; } = string.Empty;
    public string ProductNameEn { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceHT { get; set; }
    public VatRate VatRate { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Products.Product Product { get; set; } = null!;
}
