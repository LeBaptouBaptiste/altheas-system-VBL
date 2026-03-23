namespace API_Althea_systems.Models.Messaging;

public class ChatConversation
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public bool Escalated { get; set; }
    public Guid? TicketId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Users.User? User { get; set; }
    public SupportTicket? Ticket { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
