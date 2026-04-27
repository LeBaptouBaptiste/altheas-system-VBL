using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Common.Auth;
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
        var totalRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == Common.Enums.PaymentStatus.Validated)
            .SumAsync(o => o.Items.Sum(i => i.PriceHT * i.Quantity));

        var totalOrders = await _context.Orders.CountAsync();
        var totalCustomers = await _context.Users.CountAsync(u => u.Role == Common.Enums.UserRole.Customer);
        var totalProducts = await _context.Products.CountAsync();
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var dailySales = await _context.SalesAnalytics
            .OrderByDescending(s => s.Date)
            .Take(30)
            .Select(s => new SalesAnalyticsDto(s.Date, s.Revenue, s.OrderCount, s.CategoryBreakdown))
            .ToListAsync();

        return Ok(new DashboardKpiDto(totalRevenue, totalOrders, totalCustomers, totalProducts, avgOrderValue, dailySales));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<IEnumerable<SalesAnalyticsDto>>> GetSales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
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
