namespace API_Althea_systems.Models.Products;

public class Category
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionFr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
}
