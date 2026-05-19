using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API_Althea_systems.Models.Users;

namespace API_Althea_systems.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);

        // 2FA: TOTP secret is stored encrypted at rest (AES-GCM, base64).
        // Length covers IV (12B) + ciphertext (~20B for a 160-bit base32 secret) + tag (16B), base64-encoded.
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(256);

        // Stripe Customer ID — format "cus_" + 14-24 chars; 255 leaves headroom.
        // Not unique-indexed because nullable and 1-1 with user (covered by PK).
        builder.Property(u => u.StripeCustomerId).HasMaxLength(255);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.Addresses).WithOne(a => a.User).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.PaymentMethods).WithOne(p => p.User).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Orders).WithOne(o => o.User).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(u => u.RecoveryCodes).WithOne(r => r.User).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.ToTable("user_recovery_codes");

        builder.HasKey(r => r.Id);
        // BCrypt hashes are 60 chars; allow some headroom for cost-factor changes.
        builder.Property(r => r.CodeHash).HasMaxLength(100).IsRequired();

        // Speeds up lookups when verifying a recovery code (we filter by user).
        builder.HasIndex(r => r.UserId);
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Label).HasMaxLength(100).IsRequired();
        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Company).HasMaxLength(200);
        builder.Property(a => a.Street).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Street2).HasMaxLength(300);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(30);
    }
}

public class UserPaymentMethodConfiguration : IEntityTypeConfiguration<UserPaymentMethod>
{
    public void Configure(EntityTypeBuilder<UserPaymentMethod> builder)
    {
        builder.ToTable("user_payment_methods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Type).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Label).HasMaxLength(100).IsRequired();
    }
}
