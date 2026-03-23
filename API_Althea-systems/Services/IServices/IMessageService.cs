using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Services.IServices;

public interface IMessageService
{
    Task<ContactMessageDto> GetContactMessageByIdAsync(Guid id);
    Task<PaginatedResponse<ContactMessageDto>> GetAllContactMessagesAsync(int page, int pageSize);
    Task<ContactMessageDto> CreateContactMessageAsync(ContactMessageCreateRequest request);
    Task UpdateContactMessageStatusAsync(Guid id, Common.Enums.MessageStatus status);
    Task DeleteContactMessageAsync(Guid id);
}

public interface IChatService
{
    Task<ChatConversationDto> GetConversationByIdAsync(Guid id);
    Task<PaginatedResponse<ChatConversationDto>> GetAllConversationsAsync(int page, int pageSize);
    Task<ChatConversationDto> CreateConversationAsync(Guid? userId, string? email);
    Task<ChatMessageDto> AddMessageAsync(Guid conversationId, ChatMessageCreateRequest request);
}

public interface ITicketService
{
    Task<SupportTicketDto> GetByIdAsync(Guid id);
    Task<PaginatedResponse<SupportTicketDto>> GetAllAsync(int page, int pageSize);
    Task<SupportTicketDto> UpdateStatusAsync(Guid id, TicketUpdateRequest request);
}
