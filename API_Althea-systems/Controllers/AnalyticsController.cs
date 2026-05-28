using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Analytics;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin")]
[RequireStepUp(StepUpPurpose.Admin)]
public class AnalyticsController : ControllerBase
{
    private readonly AltheaDbContext _context;

    public AnalyticsController(AltheaDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardKpiDto>> GetDashboard()
    {
        // All time windows are computed against UTC so the dashboard stays
        // deterministic regardless of the admin's browser timezone.
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = todayStart.AddDays(-6);  // 7-day window includes today
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fiveWeeksStart = todayStart.AddDays(-34); // 5 × 7 = 35 days incl. today

        // ── Revenue KPIs ─────────────────────────────────
        // We only count revenue on PAID orders (PaymentStatus.Validated) —
        // pending / failed / refunded orders don't represent real income.
        // PriceHT × Quantity per item, summed across the order. No shipping
        // (shipping = pass-through, not revenue).
        var paidOrders = _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Validated);

        // Pre-fetch the data we'll aggregate multiple ways. One DB hit
        // beats four separate SUM queries on the same table.
        var paidOrderRows = await paidOrders
            .Where(o => o.Date >= fiveWeeksStart)
            .Select(o => new
            {
                o.Date,
                Revenue = o.Items.Sum(i => i.PriceHT * i.Quantity),
            })
            .ToListAsync();

        decimal RevenueIn(DateTime start) =>
            paidOrderRows.Where(r => r.Date >= start).Sum(r => r.Revenue);

        var revenueToday = RevenueIn(todayStart);
        var revenueWeek = RevenueIn(weekStart);
        var revenueMonth = RevenueIn(monthStart);

        // ── Orders today ─────────────────────────────────
        // Count ALL orders placed today, not just paid ones — admin uses
        // this to gauge activity, not turnover.
        var ordersToday = await _context.Orders
            .Where(o => o.Date >= todayStart)
            .CountAsync();

        // ── Stock alerts ────────────────────────────────
        // Both LowStock and OutOfStock count — admin should be nudged for
        // either case. Draft products excluded (Status != Active).
        var stockAlerts = await _context.Products
            .Where(p => p.Status == ProductStatus.Active
                     && (p.StockStatus == StockStatus.LowStock
                         || p.StockStatus == StockStatus.OutOfStock))
            .CountAsync();

        // ── Unread messages ─────────────────────────────
        var unreadMessages = await _context.ContactMessages
            .Where(m => m.Status == MessageStatus.Unread)
            .CountAsync();

        // ── Sales by day (last 7 days) ──────────────────
        // Group the 7-day rows we already have. Then left-join against the
        // full 7-day calendar so days with zero sales still appear (the
        // chart needs a continuous X-axis or the gaps look broken).
        var weekRows = paidOrderRows
            .Where(r => r.Date >= weekStart)
            .GroupBy(r => DateOnly.FromDateTime(r.Date.Date))
            .ToDictionary(
                g => g.Key,
                g => new { Revenue = g.Sum(x => x.Revenue), Count = g.Count() });

        var salesByDay = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var day = DateOnly.FromDateTime(weekStart.AddDays(offset));
                weekRows.TryGetValue(day, out var bucket);
                return new DailySalesDto(
                    day,
                    bucket?.Revenue ?? 0m,
                    bucket?.Count ?? 0);
            })
            .ToList();

        // ── Sales by category (last 5 weeks) ────────────
        // Join Orders → Items → Products → ProductCategories. One row per
        // (order item, category) so a multi-category product splits the
        // revenue across each category it belongs to (matches the front's
        // category badge logic). For demo seed data, most products have
        // one category so this is effectively a clean breakdown.
        var categoryRows = await _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Validated
                     && o.Date >= fiveWeeksStart)
            .SelectMany(o => o.Items.SelectMany(i =>
                i.Product.ProductCategories.Select(pc => new
                {
                    CategoryId = pc.CategoryId,
                    LineRevenue = i.PriceHT * i.Quantity,
                })))
            .ToListAsync();

        var salesByCategory = categoryRows
            .GroupBy(r => r.CategoryId.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(r => r.LineRevenue));

        // ── Lifetime totals (extra cards / API consumers) ─
        var totalRevenue = await paidOrders
            .SumAsync(o => (decimal?)o.Items.Sum(i => i.PriceHT * i.Quantity)) ?? 0m;
        var totalOrders = await _context.Orders.CountAsync();
        var totalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer);
        var totalProducts = await _context.Products.CountAsync();
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        return Ok(new DashboardKpiDto(
            RevenueToday: revenueToday,
            RevenueWeek: revenueWeek,
            RevenueMonth: revenueMonth,
            OrdersToday: ordersToday,
            StockAlerts: stockAlerts,
            UnreadMessages: unreadMessages,
            SalesByDay: salesByDay,
            SalesByCategory: salesByCategory,
            TotalRevenue: totalRevenue,
            TotalOrders: totalOrders,
            TotalCustomers: totalCustomers,
            TotalProducts: totalProducts,
            AverageOrderValue: avgOrderValue));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<IEnumerable<SalesAnalyticsDto>>> GetSales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        // Kept on the legacy SalesAnalytics table. Not used by the dashboard
        // anymore — if you want to retire it, also drop the SalesAnalytics
        // EF entity + table.
        var query = _context.SalesAnalytics.AsQueryable();

        if (from.HasValue) query = query.Where(s => s.Date >= from.Value);
        if (to.HasValue) query = query.Where(s => s.Date <= to.Value);

        var sales = await query
            .OrderBy(s => s.Date)
            .Select(s => new SalesAnalyticsDto(s.Date, s.Revenue, s.OrderCount, s.CategoryBreakdown))
            .ToListAsync();

        return Ok(sales);
    }
}
