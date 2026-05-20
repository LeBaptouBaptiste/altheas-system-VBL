using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Models.Order;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public Guid BillingAddressId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Stripe PaymentIntent ID (pi_xxx). Set when the PaymentIntent is created
    /// server-side. Used to look up the order from a webhook event and to
    /// retry / cancel the intent via the Stripe API.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Raw Stripe PaymentIntent status (requires_payment_method, processing,
    /// requires_action, succeeded, canceled…). Distinct from the domain
    /// PaymentStatus enum: this is the verbatim Stripe value, kept for
    /// reconciliation and debugging when a webhook arrives out of order.
    /// </summary>
    public string? StripePaymentStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Phase 7 (store credit): amount of store credit (in cents EUR) the
    /// customer applied to this order at checkout. Reduces the Stripe
    /// PaymentIntent amount accordingly. Set at order creation, decremented
    /// from <see cref="Users.User.CreditBalanceCents"/> only when the
    /// payment actually clears (Stripe webhook).
    /// </summary>
    public long CreditAppliedCents { get; set; }

    // Navigation
    public Users.User User { get; set; } = null!;
    public Users.Address BillingAddress { get; set; } = null!;
    public Users.Address ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusChange> StatusHistory { get; set; } = [];
    public ICollection<Invoices.Invoice> Invoices { get; set; } = [];
}
