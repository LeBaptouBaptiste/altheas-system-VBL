using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Products;

namespace API_Althea_systems.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NameFr).HasMaxLength(300).IsRequired();
        builder.Property(p => p.NameEn).HasMaxLength(300).IsRequired();
        builder.Property(p => p.DescriptionFr).HasMaxLength(1000);
        builder.Property(p => p.DescriptionEn).HasMaxLength(1000);
        builder.Property(p => p.PriceHT).HasPrecision(18, 2);
        builder.Property(p => p.VatRate).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.StockStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.HasMany(p => p.Specs).WithOne(s => s.Product).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.OrderItems).WithOne(oi => oi.Product).HasForeignKey(oi => oi.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Slug).HasMaxLength(200).IsRequired();
        builder.Property(c => c.NameFr).HasMaxLength(200).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DescriptionFr).HasMaxLength(1000);
        builder.Property(c => c.DescriptionEn).HasMaxLength(1000);
        builder.Property(c => c.Image).HasMaxLength(500);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });

        builder.HasOne(pc => pc.Product).WithMany(p => p.ProductCategories).HasForeignKey(pc => pc.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pc => pc.Category).WithMany(c => c.ProductCategories).HasForeignKey(pc => pc.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductSpecConfiguration : IEntityTypeConfiguration<ProductSpec>
{
    public void Configure(EntityTypeBuilder<ProductSpec> builder)
    {
        builder.ToTable("product_specs");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(500).IsRequired();
    }
}
