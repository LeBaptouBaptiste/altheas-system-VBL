namespace API_Althea_systems.Models.Products;

public class ProductSpec
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>French label — the canonical/default value. Required.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>French value — the canonical/default value. Required.</summary>
    public string Value { get; set; } = string.Empty;

    // Translations (nullable: front falls back to the French value when a
    // locale isn't filled in). Many spec VALUES are language-agnostic
    // (e.g. "3 008 × 3 008 px") — those usually mirror the French value.
    public string? LabelEn { get; set; }
    public string? LabelMs { get; set; }
    public string? LabelAr { get; set; }
    public string? ValueEn { get; set; }
    public string? ValueMs { get; set; }
    public string? ValueAr { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
