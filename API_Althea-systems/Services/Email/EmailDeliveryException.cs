namespace API_Althea_systems.Services.Email;

/// <summary>
/// Thrown when the SMTP transport rejects a message (connection refused, auth
/// failed, recipient rejected, etc.). Callers should catch this specifically
/// so they can decide whether to fail the request or log + continue (e.g. the
/// Stripe webhook keeps marking the order as paid even if the receipt mail
/// failed — the payment IS valid).
/// </summary>
public class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message, Exception innerException)
        : base(message, innerException) { }
}
