using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;

    public MessageService(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<ContactMessageDto> GetContactMessageByIdAsync(Guid id)
    {
        var msg = await _messageRepository.GetContactMessageByIdAsync(id)
            ?? throw new NotFoundException("ContactMessage", id);
        return MapToDto(msg);
    }

    public async Task<PaginatedResponse<ContactMessageDto>> GetAllContactMessagesAsync(int page, int pageSize)
    {
        var msgs = await _messageRepository.GetAllContactMessagesAsync(page, pageSize);
        var total = await _messageRepository.CountContactMessagesAsync();
        return new PaginatedResponse<ContactMessageDto>(
            msgs.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ContactMessageDto> CreateContactMessageAsync(ContactMessageCreateRequest request)
    {
        var msg = new ContactMessage
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Subject = request.Subject,
            Message = request.Message
        };
        await _messageRepository.CreateContactMessageAsync(msg);
        return MapToDto(msg);
    }

    public async Task UpdateContactMessageStatusAsync(Guid id, Common.Enums.MessageStatus status)
    {
        var msg = await _messageRepository.GetContactMessageByIdAsync(id)
            ?? throw new NotFoundException("ContactMessage", id);
        msg.Status = status;
        await _messageRepository.UpdateContactMessageAsync(msg);
    }

    public async Task DeleteContactMessageAsync(Guid id)
    {
        var msg = await _messageRepository.GetContactMessageByIdAsync(id)
            ?? throw new NotFoundException("ContactMessage", id);
        await _messageRepository.DeleteContactMessageAsync(msg);
    }

    private static ContactMessageDto MapToDto(ContactMessage m) => new(
        m.Id, m.Email, m.Subject, m.Message, m.Status, m.CreatedAt);
}

public class ChatService : IChatService
{
    private readonly IMessageRepository _messageRepository;

    public ChatService(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<ChatConversationDto> GetConversationByIdAsync(Guid id)
    {
        var conv = await _messageRepository.GetConversationByIdAsync(id)
            ?? throw new NotFoundException("ChatConversation", id);
        return MapToDto(conv);
    }

    public async Task<PaginatedResponse<ChatConversationDto>> GetAllConversationsAsync(int page, int pageSize)
    {
        var convs = await _messageRepository.GetAllConversationsAsync(page, pageSize);
        var total = await _messageRepository.CountConversationsAsync();
        return new PaginatedResponse<ChatConversationDto>(
            convs.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ChatConversationDto> CreateConversationAsync(Guid? userId, string? email)
    {
        var conv = new ChatConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email
        };
        await _messageRepository.CreateConversationAsync(conv);
        return MapToDto(conv);
    }

    public async Task<ChatMessageDto> AddMessageAsync(Guid conversationId, ChatMessageCreateRequest request)
    {
        _ = await _messageRepository.GetConversationByIdAsync(conversationId)
            ?? throw new NotFoundException("ChatConversation", conversationId);

        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = request.Content
        };

        await _messageRepository.AddChatMessageAsync(msg);
        return new ChatMessageDto(msg.Id, msg.Role, msg.Content, msg.Timestamp);
    }

    private static ChatConversationDto MapToDto(ChatConversation c) => new(
        c.Id, c.UserId, c.Email, c.Escalated, c.TicketId, c.CreatedAt,
        c.Messages.Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.Timestamp)));
}

public class TicketService : ITicketService
{
    private readonly IMessageRepository _messageRepository;

    public TicketService(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<SupportTicketDto> GetByIdAsync(Guid id)
    {
        var ticket = await _messageRepository.GetTicketByIdAsync(id)
            ?? throw new NotFoundException("SupportTicket", id);
        return MapToDto(ticket);
    }

    public async Task<PaginatedResponse<SupportTicketDto>> GetAllAsync(int page, int pageSize)
    {
        var tickets = await _messageRepository.GetAllTicketsAsync(page, pageSize);
        var total = await _messageRepository.CountTicketsAsync();
        return new PaginatedResponse<SupportTicketDto>(
            tickets.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<SupportTicketDto> UpdateStatusAsync(Guid id, TicketUpdateRequest request)
    {
        var ticket = await _messageRepository.GetTicketByIdAsync(id)
            ?? throw new NotFoundException("SupportTicket", id);
        ticket.Status = request.Status;
        await _messageRepository.UpdateTicketAsync(ticket);
        return MapToDto(ticket);
    }

    private static SupportTicketDto MapToDto(SupportTicket t) => new(
        t.Id, t.ConversationId, t.ContactMessageId, t.Email, t.Subject, t.Status, t.CreatedAt, t.UpdatedAt);
}
