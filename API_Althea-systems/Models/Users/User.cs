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
    /// Which 2FA method the user has configured.
    /// Invariant maintained by TwoFactorService:
    ///   <c>TwoFactorEnabled == false  ⇔  TwoFactorMethod == None</c>.
    /// Both fields are kept (rather than collapsing to the enum alone) so
    /// the existing UI bool check + the front's emailConfirmed/2FA gates
    /// stay readable.
    /// </summary>
    public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;

    /// <summary>
    /// Stripe Customer ID (cus_xxx). Created on first payment, persisted so
    /// subsequent PaymentIntents reuse the same customer and saved payment
    /// methods stay attached.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Phase 7 (store credit): available credit balance in cents (EUR).
    /// Increments on a credit note issued with Mode=StoreCredit; decrements
    /// when the customer applies credit at checkout (after the
    /// PaymentIntent succeeds — abandoned carts don't burn balance).
    /// Stored as a long (no rounding ambiguity) instead of decimal —
    /// money math is cleanest in the smallest currency unit.
    /// </summary>
    public long CreditBalanceCents { get; set; }

    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Address> Addresses { get; set; } = [];
    public ICollection<UserPaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Order.Order> Orders { get; set; } = [];
    public ICollection<UserRecoveryCode> RecoveryCodes { get; set; } = [];
}
