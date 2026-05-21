using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Content;

namespace API_Althea_systems.Data.Configurations;

public class HeroSlideConfiguration : IEntityTypeConfiguration<HeroSlide>
{
    public void Configure(EntityTypeBuilder<HeroSlide> builder)
    {
        builder.ToTable("hero_slides");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Image).HasMaxLength(500).IsRequired();
        builder.Property(h => h.TitleFr).HasMaxLength(200).IsRequired();
        builder.Property(h => h.TitleEn).HasMaxLength(200).IsRequired();
        builder.Property(h => h.TitleMs).HasMaxLength(200);
        builder.Property(h => h.TitleAr).HasMaxLength(200);
        builder.Property(h => h.SubtitleFr).HasMaxLength(300);
        builder.Property(h => h.SubtitleEn).HasMaxLength(300);
        builder.Property(h => h.SubtitleMs).HasMaxLength(300);
        builder.Property(h => h.SubtitleAr).HasMaxLength(300);
        builder.Property(h => h.DescriptionFr).HasMaxLength(1000);
        builder.Property(h => h.DescriptionEn).HasMaxLength(1000);
        builder.Property(h => h.DescriptionMs).HasMaxLength(1000);
        builder.Property(h => h.DescriptionAr).HasMaxLength(1000);
        builder.Property(h => h.CtaFr).HasMaxLength(100);
        builder.Property(h => h.CtaEn).HasMaxLength(100);
        builder.Property(h => h.CtaMs).HasMaxLength(100);
        builder.Property(h => h.CtaAr).HasMaxLength(100);
        builder.Property(h => h.Link).HasMaxLength(500);
    }
}

public class StaticPageConfiguration : IEntityTypeConfiguration<StaticPage>
{
    public void Configure(EntityTypeBuilder<StaticPage> builder)
    {
        builder.ToTable("static_pages");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.Property(p => p.TitleFr).HasMaxLength(300).IsRequired();
        builder.Property(p => p.TitleEn).HasMaxLength(300).IsRequired();
        builder.Property(p => p.TitleMs).HasMaxLength(300);
        builder.Property(p => p.TitleAr).HasMaxLength(300);
        // ContentMs / ContentAr unbounded (text), same as ContentFr/En.

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
