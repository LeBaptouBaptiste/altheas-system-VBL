namespace API_Althea_systems.Models.Analytics;

public record SalesAnalyticsDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount,
    Dictionary<string, decimal> CategoryBreakdown
);

public record DashboardKpiDto(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    decimal AverageOrderValue,
    IEnumerable<SalesAnalyticsDto> DailySales
);
