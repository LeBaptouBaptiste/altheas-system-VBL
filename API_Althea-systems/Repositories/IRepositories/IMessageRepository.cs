using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Repositories.IRepositories;

public interface IMessageRepository
{
    // Contact Messages
    Task<ContactMessage?> GetContactMessageByIdAsync(Guid id);
    Task<IEnumerable<ContactMessage>> GetAllContactMessagesAsync(int page, int pageSize);
    Task<int> CountContactMessagesAsync();
    Task<ContactMessage> CreateContactMessageAsync(ContactMessage message);
    Task UpdateContactMessageAsync(ContactMessage message);
    Task DeleteContactMessageAsync(ContactMessage message);

    // Chat Conversations
    Task<ChatConversation?> GetConversationByIdAsync(Guid id);
    Task<IEnumerable<ChatConversation>> GetAllConversationsAsync(int page, int pageSize);
    Task<int> CountConversationsAsync();
    Task<ChatConversation> CreateConversationAsync(ChatConversation conversation);
    Task AddChatMessageAsync(ChatMessage message);

    // Support Tickets
    Task<SupportTicket?> GetTicketByIdAsync(Guid id);
    Task<IEnumerable<SupportTicket>> GetAllTicketsAsync(int page, int pageSize);
    Task<int> CountTicketsAsync();
    Task<SupportTicket> CreateTicketAsync(SupportTicket ticket);
    Task UpdateTicketAsync(SupportTicket ticket);
}
