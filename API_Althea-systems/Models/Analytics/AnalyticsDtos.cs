namespace API_Althea_systems.Models.Analytics;

/// <summary>
/// A single point on the "sales by day" bar chart. Aggregated from
/// <c>Orders</c> directly (no dependency on the unused SalesAnalytics
/// table). Date is UTC.
/// </summary>
public record DailySalesDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount
);

/// <summary>
/// Legacy shape kept for the /analytics/sales endpoint which still pulls
/// from the SalesAnalytics table. Not used by the dashboard anymore.
/// </summary>
public record SalesAnalyticsDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount,
    Dictionary<string, decimal> CategoryBreakdown
);

/// <summary>
/// Dashboard KPI payload. Each field maps to a card or chart on the
/// admin home page — no front-side massaging needed beyond formatting.
/// </summary>
public record DashboardKpiDto(
    // ── KPI cards (top row) ─────────────────────────────
    /// <summary>Revenue (HT) on orders placed since UTC midnight today.</summary>
    decimal RevenueToday,
    /// <summary>Revenue (HT) on orders placed in the last 7 days (rolling).</summary>
    decimal RevenueWeek,
    /// <summary>Revenue (HT) on orders placed in the current calendar month (UTC).</summary>
    decimal RevenueMonth,
    /// <summary>Order count since UTC midnight today.</summary>
    int OrdersToday,
    /// <summary>Products with StockStatus = LowStock or OutOfStock.</summary>
    int StockAlerts,
    /// <summary>Contact messages with Status = Unread.</summary>
    int UnreadMessages,

    // ── Charts ──────────────────────────────────────────
    /// <summary>
    /// One entry per day for the last 7 days (today included). Drives the
    /// "Ventes par jour" bar chart. Days with zero sales still appear with
    /// Revenue=0/OrderCount=0 so the X-axis is continuous.
    /// </summary>
    IEnumerable<DailySalesDto> SalesByDay,

    /// <summary>
    /// CategoryId → revenue HT over the last 5 weeks. Drives the
    /// "Ventes par catégorie" pie chart. Categories with zero sales are
    /// omitted (the front only renders non-zero slices).
    /// </summary>
    Dictionary<string, decimal> SalesByCategory,

    // ── Totals (extra, kept for completeness — not displayed) ──
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    decimal AverageOrderValue
);
