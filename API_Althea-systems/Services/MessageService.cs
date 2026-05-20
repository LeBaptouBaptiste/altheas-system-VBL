using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;
using API_Althea_systems.Services.Ollama;

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
    private readonly IOllamaService _ollama;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IMessageRepository messageRepository,
        IOllamaService ollama,
        ILogger<ChatService> logger)
    {
        _messageRepository = messageRepository;
        _ollama = ollama;
        _logger = logger;
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
        var conv = await _messageRepository.GetConversationByIdAsync(conversationId)
            ?? throw new NotFoundException("ChatConversation", conversationId);

        // 1. Persist the user message first so it's never lost even if Ollama
        //    blows up later. The conversation history needs it on disk before
        //    we ship it to the model.
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = request.Content
        };
        await _messageRepository.AddChatMessageAsync(userMsg);

        // 2. Build the running history (oldest first) and call Ollama.
        //    Past messages from the DB + the user message we just stored.
        var history = conv.Messages
            .OrderBy(m => m.Timestamp)
            .Select(m => (
                Role: m.Role == ChatRole.User ? "user" : "assistant",
                Content: m.Content))
            .ToList();
        history.Add(("user", request.Content));

        string reply;
        try
        {
            reply = await _ollama.GenerateReplyAsync(history);
        }
        catch (OllamaModelNotReadyException ex)
        {
            // Model isn't pulled yet (common right after `docker compose up`
            // while ollama-init is still downloading the ~2 GB blob).
            // Distinct copy so the user knows to retry vs. give up.
            _logger.LogWarning(ex,
                "Ollama model not pulled yet for conversation {ConversationId}.",
                conversationId);
            reply = "Notre assistant est en cours d'initialisation (téléchargement du modèle). " +
                    "Réessayez dans une à deux minutes — la prochaine question fonctionnera.";
        }
        catch (Exception ex)
        {
            // Don't let an LLM outage break the customer's chat — surface a
            // polite fallback that still gets persisted (so the admin can
            // see the failure in the conversation transcript and follow up).
            _logger.LogError(ex,
                "Ollama call failed for conversation {ConversationId}; returning fallback reply.",
                conversationId);
            reply = "Désolé, notre assistant n'est pas disponible pour l'instant. " +
                    "Vous pouvez créer un ticket support et un membre de notre équipe vous répondra rapidement.";
        }

        // 3. Persist the bot reply. The endpoint returns the BOT message —
        //    the front already shows the user's input optimistically, so it
        //    just needs the assistant's answer to append.
        var botMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = ChatRole.Bot,
            Content = reply
        };
        await _messageRepository.AddChatMessageAsync(botMsg);

        return new ChatMessageDto(botMsg.Id, botMsg.Role, botMsg.Content, botMsg.Timestamp);
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
