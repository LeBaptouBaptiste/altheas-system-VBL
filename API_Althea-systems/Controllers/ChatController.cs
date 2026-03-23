using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ChatConversationDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _chatService.GetAllConversationsAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChatConversationDto>> GetById(Guid id)
    {
        return Ok(await _chatService.GetConversationByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ChatConversationDto>> Create([FromQuery] Guid? userId, [FromQuery] string? email)
    {
        var conv = await _chatService.CreateConversationAsync(userId, email);
        return CreatedAtAction(nameof(GetById), new { id = conv.Id }, conv);
    }

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<ActionResult<ChatMessageDto>> AddMessage(Guid conversationId, [FromBody] ChatMessageCreateRequest request)
    {
        return Created("", await _chatService.AddMessageAsync(conversationId, request));
    }
}
