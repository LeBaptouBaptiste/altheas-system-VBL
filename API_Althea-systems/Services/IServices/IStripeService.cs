using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using Stripe;

namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Thin wrapper around the Stripe SDK. Responsible for:
///   • mapping our domain (User, Order) to Stripe's API,
///   • computing payment amounts AUTHORITATIVELY from the server,
///   • persisting Stripe-side identifiers (StripeCustomerId) back on
///     the user so subsequent payments reuse the same Customer.
///
/// The amount of a PaymentIntent is NEVER trusted from the client: it
/// comes from the order's items and shipping cost via the VAT service.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Returns the user's Stripe Customer ID, creating one if needed.
    /// Persists the new ID on User.StripeCustomerId so subsequent calls
    /// are no-ops on the Stripe side.
    /// </summary>
    Task<string> GetOrCreateCustomerAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Creates a PaymentIntent for the order's total TTC (incl. VAT and
    /// shipping). Attaches it to the user's Stripe customer.
    /// If <paramref name="saveCard"/> is true, the intent is created with
    /// <c>setup_future_usage = "off_session"</c> so the payment method
    /// can be reused later.
    /// </summary>
    /// <returns>The created PaymentIntent. Caller is expected to send
    /// <c>ClientSecret</c> to the frontend for confirmation.</returns>
    Task<PaymentIntent> CreatePaymentIntentAsync(
        Order order,
        User user,
        bool saveCard,
        CancellationToken ct = default);
}
