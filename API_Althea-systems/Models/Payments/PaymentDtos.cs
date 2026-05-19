namespace API_Althea_systems.Models.Payments;

/// <summary>
/// Client-submitted request to start the Stripe payment flow for an
/// existing order. The order MUST already exist (created by
/// POST /api/orders) and have PaymentMethod = Card; the amount is
/// recomputed server-side from order.Items.
/// </summary>
public record CreatePaymentIntentRequest(
    Guid OrderId,
    /// <summary>
    /// If true, the resulting PaymentIntent is created with
    /// setup_future_usage = "off_session", letting us charge the same
    /// card later without re-prompting the user.
    /// </summary>
    bool SaveCard
);

/// <summary>
/// Response sent back to the frontend. The ClientSecret is what
/// stripe.confirmPayment() needs on the browser; never log or expose
/// it beyond the user's session.
/// </summary>
public record CreatePaymentIntentResponse(
    string ClientSecret,
    string PaymentIntentId,
    long Amount,
    string Currency
);
