using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Repositories.IRepositories;

namespace API_Althea_systems.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AltheaDbContext _context;

    public MessageRepository(AltheaDbContext context)
    {
        _context = context;
    }

    // ── Contact Messages ─────────────────────────────────

    public async Task<ContactMessage?> GetContactMessageByIdAsync(Guid id)
        => await _context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<ContactMessage>> GetAllContactMessagesAsync(int page, int pageSize)
        => await _context.ContactMessages
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<int> CountContactMessagesAsync()
        => await _context.ContactMessages.CountAsync();

    public async Task<ContactMessage> CreateContactMessageAsync(ContactMessage message)
    {
        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task UpdateContactMessageAsync(ContactMessage message)
    {
        _context.ContactMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteContactMessageAsync(ContactMessage message)
    {
        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync();
    }

    // ── Chat Conversations ───────────────────────────────

    public async Task<ChatConversation?> GetConversationByIdAsync(Guid id)
        => await _context.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<ChatConversation>> GetAllConversationsAsync(int page, int pageSize)
        => await _context.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<int> CountConversationsAsync()
        => await _context.ChatConversations.CountAsync();

    public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation)
    {
        _context.ChatConversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task AddChatMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }

    // ── Support Tickets ──────────────────────────────────

    public async Task<SupportTicket?> GetTicketByIdAsync(Guid id)
        => await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<SupportTicket>> GetAllTicketsAsync(int page, int pageSize)
        => await _context.SupportTickets
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<int> CountTicketsAsync()
        => await _context.SupportTickets.CountAsync();

    public async Task<SupportTicket> CreateTicketAsync(SupportTicket ticket)
    {
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task UpdateTicketAsync(SupportTicket ticket)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }
}
