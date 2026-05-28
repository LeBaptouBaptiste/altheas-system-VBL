using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<OrderDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] Guid? userId = null)
    {
        // Non-admin users can only see their own orders.
        var currentUserId = HttpContext.GetCurrentUserId()
            ?? throw new ForbiddenException("Authentication required.");

        if (!HttpContext.IsCurrentUserAdmin())
            userId = currentUserId;

        return Ok(await _orderService.GetAllAsync(page, pageSize, userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        // Service throws NotFoundException for unknown ids; once we have the
        // order, enforce ownership (or admin bypass) before returning it.
        var order = await _orderService.GetByIdAsync(id);
        HttpContext.RequireOwnershipOrAdmin(order.UserId);
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] OrderCreateRequest request)
    {
        var userId = HttpContext.GetCurrentUserId()
            ?? throw new ForbiddenException("Authentication required.");
        var order = await _orderService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] OrderStatusUpdateRequest request)
    {
        var userId = HttpContext.GetCurrentUserId()
            ?? throw new ForbiddenException("Authentication required.");
        return Ok(await _orderService.UpdateStatusAsync(id, userId, request));
    }
}
