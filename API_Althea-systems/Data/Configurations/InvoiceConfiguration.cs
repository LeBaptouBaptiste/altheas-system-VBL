using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Invoices;

namespace API_Althea_systems.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);
        // {ClientCode 8}-{YYYY 4}-{MM 2}-{NNNN 4} = 22 chars exactly; 40 leaves
        // room for future format tweaks (longer client code, suffixes, etc.)
        // without a migration. Unique index enforces non-duplicate numbers
        // even under race conditions — InvoiceService retries on collision.
        builder.Property(i => i.Number).HasMaxLength(40).IsRequired();
        builder.HasIndex(i => i.Number).IsUnique();
        builder.Property(i => i.AmountHT).HasPrecision(18, 2);
        builder.Property(i => i.VatAmount).HasPrecision(18, 2);
        builder.Property(i => i.AmountTTC).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Type).HasConversion<string>().HasMaxLength(20);

        // Phase 7: credit-note specific fields.
        builder.Property(i => i.Mode).HasConversion<string>().HasMaxLength(20);
        // Stripe refund ids are "re_" + 24 chars; 255 leaves comfortable headroom.
        builder.Property(i => i.StripeRefundId).HasMaxLength(255);
        builder.Property(i => i.RefundStatus).HasMaxLength(30);
        // Index for the webhook handler's lookup-by-refund-id (charge.refunded
        // event arrives → we find the matching Invoice).
        builder.HasIndex(i => i.StripeRefundId);

        builder.HasOne(i => i.RelatedInvoice).WithMany().HasForeignKey(i => i.RelatedInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
