using API_Althea_systems.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Althea_systems.Data.Configurations;

public class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.ToTable("email_confirmation_tokens");

        builder.HasKey(t => t.Id);

        // SHA-256 hex = 64 chars exactly. Unique index — the only way two
        // entries collide is a SHA-256 preimage, in which case we have
        // bigger problems than email confirmation.
        builder.Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Cascade delete: if the User row goes away (GDPR anonymize, etc.),
        // any unconsumed token for them is meaningless and must go too.
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Per-user index speeds up the "rate-limit resend" lookup
        // (latest token issued for this user within the cooldown window).
        builder.HasIndex(t => t.UserId);
    }
}
