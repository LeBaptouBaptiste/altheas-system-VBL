using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Listing all conversations across the system is admin-only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<PaginatedResponse<ChatConversationDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _chatService.GetAllConversationsAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChatConversationDto>> GetById(Guid id)
    {
        var conv = await _chatService.GetConversationByIdAsync(id);
        // Anonymous-owned conversations (UserId == null, only an email) are
        // an artefact of the legacy public chatbot; admins can still inspect
        // them but no regular user owns them, so deny by default.
        if (!HttpContext.IsCurrentUserAdmin())
        {
            if (conv.UserId is null)
                throw new ForbiddenException("You are not allowed to access this resource.");
            HttpContext.RequireOwnershipOrAdmin(conv.UserId.Value);
        }
        return Ok(conv);
    }

    [HttpPost]
    public async Task<ActionResult<ChatConversationDto>> Create([FromQuery] Guid? userId, [FromQuery] string? email)
    {
        // Force the conversation owner to be the authenticated caller
        // (admins may impersonate via the explicit query parameter).
        if (!HttpContext.IsCurrentUserAdmin())
        {
            userId = HttpContext.GetCurrentUserId()
                ?? throw new ForbiddenException("Authentication required.");
        }

        var conv = await _chatService.CreateConversationAsync(userId, email);
        return CreatedAtAction(nameof(GetById), new { id = conv.Id }, conv);
    }

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<ActionResult<ChatMessageDto>> AddMessage(Guid conversationId, [FromBody] ChatMessageCreateRequest request)
    {
        if (!HttpContext.IsCurrentUserAdmin())
        {
            var conv = await _chatService.GetConversationByIdAsync(conversationId);
            if (conv.UserId is null)
                throw new ForbiddenException("You are not allowed to access this resource.");
            HttpContext.RequireOwnershipOrAdmin(conv.UserId.Value);
        }
        return Created("", await _chatService.AddMessageAsync(conversationId, request));
    }
}
