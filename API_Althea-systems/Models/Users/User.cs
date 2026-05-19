using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Users;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool Anonymized { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? TwoFactorEnabledAt { get; set; }

    /// <summary>
    /// Stripe Customer ID (cus_xxx). Created on first payment, persisted so
    /// subsequent PaymentIntents reuse the same customer and saved payment
    /// methods stay attached.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Address> Addresses { get; set; } = [];
    public ICollection<UserPaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Order.Order> Orders { get; set; } = [];
    public ICollection<UserRecoveryCode> RecoveryCodes { get; set; } = [];
}
