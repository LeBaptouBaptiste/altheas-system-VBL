using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Roles = "Admin")]
[RequireStepUp(StepUpPurpose.Admin)]
public class TicketController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SupportTicketDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _ticketService.GetAllAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> GetById(Guid id)
    {
        return Ok(await _ticketService.GetByIdAsync(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> UpdateStatus(Guid id, [FromBody] TicketUpdateRequest request)
    {
        return Ok(await _ticketService.UpdateStatusAsync(id, request));
    }
}
