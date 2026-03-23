namespace API_Althea_systems.Models.Products;

public class ProductSpec
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // Navigation
    public Product Product { get; set; } = null!;
}
