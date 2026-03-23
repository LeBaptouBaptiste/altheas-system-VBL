using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Messaging;

public class ContactMessage
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageStatus Status { get; set; } = MessageStatus.Unread;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SupportTicket? Ticket { get; set; }
}
