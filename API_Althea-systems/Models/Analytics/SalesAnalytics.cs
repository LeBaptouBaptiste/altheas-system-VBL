namespace API_Althea_systems.Models.Analytics;

public class SalesAnalytics
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
