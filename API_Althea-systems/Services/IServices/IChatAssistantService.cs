using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Services.IServices;

public interface IChatAssistantService
{
    Task<ChatMessageDto> GenerateReplyAsync(Guid conversationId, ChatMessageCreateRequest request);
}