using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Payments;

namespace API_Althea_systems.Data.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");

        // EventId is the PK — its uniqueness IS the idempotency guarantee.
        builder.HasKey(w => w.EventId);
        // Stripe event ids are ~28 chars (evt_ + 24 base62), 100 is comfortable headroom.
        builder.Property(w => w.EventId).HasMaxLength(100);
        builder.Property(w => w.Type).HasMaxLength(100).IsRequired();
        builder.Property(w => w.ProcessedAt).IsRequired();

        // Secondary index for ops queries like "show me all succeeded events
        // in the last hour". Cheap (low-cardinality column).
        builder.HasIndex(w => w.Type);
    }
}
