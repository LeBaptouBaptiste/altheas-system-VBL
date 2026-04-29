namespace API_Althea_systems.Common.Exceptions;

public class UnauthorizedException : Exception
{
    /// <summary>
    /// Optional machine-readable reason carried alongside the message.
    /// The handler echoes it in the JSON body so front-ends can branch
    /// (e.g. "invalid_credentials" must NOT trigger the localStorage
    /// token wipe — only a bad/expired JWT should).
    /// </summary>
    public string? Reason { get; }

    public UnauthorizedException(string message = "Unauthorized access.", string? reason = null)
        : base(message)
    {
        Reason = reason;
    }
}
