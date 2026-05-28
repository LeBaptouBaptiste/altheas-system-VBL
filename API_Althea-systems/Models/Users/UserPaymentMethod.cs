namespace API_Althea_systems.Models.Users;

/// <summary>
/// A payment method saved on the user's profile for reuse at checkout.
/// For card methods (the only kind today), the source of truth is Stripe:
/// our row only stores display metadata (brand, last4, expiry) — never
/// the PAN or CVC. Charging happens via Stripe with the StripePaymentMethodId.
/// </summary>
public class UserPaymentMethod
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Currently always "card". Left as string for future SEPA / bank methods.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Display label, e.g. "Visa •••• 4242". Computed on persist.</summary>
    public string Label { get; set; } = string.Empty;

    // ── Stripe-side identifier ────────────────────────────────────
    /// <summary>Stripe PaymentMethod id (pm_xxx). Use it to charge later.</summary>
    public string? StripePaymentMethodId { get; set; }

    // ── Display metadata (NEVER sensitive — these come from Stripe, safe to log) ─
    /// <summary>Card brand: "visa" | "mastercard" | "amex" | "discover" | "diners" | "jcb" | "unionpay" | "unknown".</summary>
    public string? Brand { get; set; }
    /// <summary>Last 4 digits of the card, exactly 4 chars. Safe to display.</summary>
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
