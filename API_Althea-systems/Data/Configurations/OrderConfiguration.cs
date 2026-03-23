using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Order;

namespace API_Althea_systems.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.ShippingMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2);

        builder.HasOne(o => o.BillingAddress).WithMany().HasForeignKey(o => o.BillingAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.StatusHistory).WithOne(s => s.Order).HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Invoices).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.ProductNameFr).HasMaxLength(300).IsRequired();
        builder.Property(oi => oi.ProductNameEn).HasMaxLength(300).IsRequired();
        builder.Property(oi => oi.PriceHT).HasPrecision(18, 2);
        builder.Property(oi => oi.VatRate).HasConversion<string>().HasMaxLength(20);
    }
}

public class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("order_status_changes");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.From).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.To).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.ChangedBy).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
