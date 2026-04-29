using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/messages")]
[EnableRateLimiting("contact")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<PaginatedResponse<ContactMessageDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _messageService.GetAllContactMessagesAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<ContactMessageDto>> GetById(Guid id)
    {
        return Ok(await _messageService.GetContactMessageByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ContactMessageDto>> Create([FromBody] ContactMessageCreateRequest request)
    {
        var msg = await _messageService.CreateContactMessageAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = msg.Id }, msg);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MessageStatus status)
    {
        await _messageService.UpdateContactMessageStatusAsync(id, status);
        return Ok(new { message = "Status updated." });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _messageService.DeleteContactMessageAsync(id);
        return NoContent();
    }
}
