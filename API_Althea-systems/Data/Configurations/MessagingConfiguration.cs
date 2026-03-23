using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Data.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Email).HasMaxLength(320).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Message).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
    }
}

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Email).HasMaxLength(320);

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.Content).IsRequired();
    }
}

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Email).HasMaxLength(320).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(t => t.Conversation).WithOne(c => c.Ticket).HasForeignKey<SupportTicket>(t => t.ConversationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.ContactMessage).WithOne(m => m.Ticket).HasForeignKey<SupportTicket>(t => t.ContactMessageId).OnDelete(DeleteBehavior.SetNull);
    }
}
