using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Messaging;

public class SupportTicket
{
    public Guid Id { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? ContactMessageId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatConversation? Conversation { get; set; }
    public ContactMessage? ContactMessage { get; set; }
}
