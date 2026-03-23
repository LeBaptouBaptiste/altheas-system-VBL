namespace API_Althea_systems.Models.Messaging;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatConversation Conversation { get; set; } = null!;
}

public enum ChatRole
{
    User,
    Bot
}
