namespace API_Althea_systems.Models.Products;

public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Guid CategoryId { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
