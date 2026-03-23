using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Analytics;

namespace API_Althea_systems.Data.Configurations;

public class SalesAnalyticsConfiguration : IEntityTypeConfiguration<SalesAnalytics>
{
    public void Configure(EntityTypeBuilder<SalesAnalytics> builder)
    {
        builder.ToTable("sales_analytics");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Revenue).HasPrecision(18, 2);

        // Store CategoryBreakdown as JSONB in PostgreSQL
        builder.Property(s => s.CategoryBreakdown)
            .HasColumnType("jsonb");

        builder.HasIndex(s => s.Date).IsUnique();
    }
}
