using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Messaging;

public record ContactMessageDto(
    Guid Id,
    string Email,
    string Subject,
    string Message,
    MessageStatus Status,
    DateTime CreatedAt
);

public record ContactMessageCreateRequest(
    string Email,
    string Subject,
    string Message
);

public record ChatConversationDto(
    Guid Id,
    Guid? UserId,
    string? Email,
    bool Escalated,
    Guid? TicketId,
    DateTime CreatedAt,
    IEnumerable<ChatMessageDto> Messages
);

public record ChatMessageDto(
    Guid Id,
    ChatRole Role,
    string Content,
    DateTime Timestamp
);

public record ChatMessageCreateRequest(string Content);

public record SupportTicketDto(
    Guid Id,
    Guid? ConversationId,
    Guid? ContactMessageId,
    string Email,
    string Subject,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketUpdateRequest(TicketStatus Status);
