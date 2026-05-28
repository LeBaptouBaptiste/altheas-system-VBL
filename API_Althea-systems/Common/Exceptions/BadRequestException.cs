namespace API_Althea_systems.Common.Exceptions;

/// <summary>
/// Domain-level 400 Bad Request, distinct from <see cref="AppValidationException"/>
/// (which carries per-field validator errors). Used when the request shape
/// is fine but the business rule rejects it — e.g. an expired confirmation
/// link, a token that has already been consumed.
///
/// Carries an optional machine-readable <see cref="Reason"/> so front-ends
/// can branch on it (e.g. "token_expired" → show "request a new one",
/// "token_consumed" → show "go to login").
/// </summary>
public class BadRequestException : Exception
{
    public string? Reason { get; }

    public BadRequestException(string message, string? reason = null) : base(message)
    {
        Reason = reason;
    }
}
