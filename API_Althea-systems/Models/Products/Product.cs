using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    // MS / AR are nullable on purpose: the front falls back to FR when a
    // localised value is missing, so older products without translations
    // keep working after the migration.
    public string? NameMs { get; set; }
    public string? NameAr { get; set; }
    public string DescriptionFr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string? DescriptionMs { get; set; }
    public string? DescriptionAr { get; set; }
    public string LongDescriptionFr { get; set; } = string.Empty;
    public string LongDescriptionEn { get; set; } = string.Empty;
    public string? LongDescriptionMs { get; set; }
    public string? LongDescriptionAr { get; set; }
    public decimal PriceHT { get; set; }
    public VatRate VatRate { get; set; } = VatRate.Standard;
    public int StockQty { get; set; }
    public StockStatus StockStatus { get; set; } = StockStatus.InStock;
    public bool IsNew { get; set; }
    public int PriorityRank { get; set; }
    public string[] Images { get; set; } = [];
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
    public ICollection<ProductSpec> Specs { get; set; } = [];
    public ICollection<Order.OrderItem> OrderItems { get; set; } = [];
}
