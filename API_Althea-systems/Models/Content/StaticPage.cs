namespace API_Althea_systems.Models.Content;

public class StaticPage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleFr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string ContentFr { get; set; } = string.Empty;
    public string ContentEn { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
