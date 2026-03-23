namespace API_Althea_systems.Models.Content;

public class HeroSlide
{
    public Guid Id { get; set; }
    public string Image { get; set; } = string.Empty;
    public string TitleFr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SubtitleFr { get; set; } = string.Empty;
    public string SubtitleEn { get; set; } = string.Empty;
    public string DescriptionFr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string CtaFr { get; set; } = string.Empty;
    public string CtaEn { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
