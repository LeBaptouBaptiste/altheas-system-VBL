namespace API_Althea_systems.Common.Exceptions;

/// <summary>
/// Thrown when an account is temporarily locked because too many failed
/// 2FA / step-up attempts have piled up. The handler maps this to HTTP 429
/// with a <c>Retry-After</c> header and a body carrying
/// <c>{ reason: "account_locked", retryAfterSeconds }</c>.
/// </summary>
public class AccountLockedException : Exception
{
    public int RetryAfterSeconds { get; }

    public AccountLockedException(int retryAfterSeconds, string? message = null)
        : base(message ?? "Account temporarily locked due to too many failed attempts. Try again later.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
