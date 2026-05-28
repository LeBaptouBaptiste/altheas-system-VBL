namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Tracks login failures keyed by email so we can rate-limit brute-force
/// attempts that span multiple IP addresses (the IP-based RateLimiter
/// would otherwise let a botnet of distinct IPs grind through passwords).
///
/// Mirrors the pattern used by <see cref="ITwoFactorStateStore"/>:
/// counter with TTL + a separate lock key so an attacker who hits the
/// threshold gets a fixed cooldown instead of a sliding one.
///
/// All operations are best-effort: if the underlying Redis is unavailable,
/// implementations should fail open (no extra throttling) rather than
/// preventing all logins. The IP-based RateLimiter remains as defence in
/// depth.
/// </summary>
public interface ILoginAttemptStore
{
    /// <summary>
    /// Returns the remaining lock TTL when the account is currently locked,
    /// or <c>null</c> when the account is free to attempt a login.
    /// </summary>
    Task<TimeSpan?> GetLockRemainingAsync(string email);

    /// <summary>
    /// Increments the failure counter (with a sliding window TTL) and returns
    /// the new count.
    /// </summary>
    Task<int> IncrementFailureAsync(string email, TimeSpan ttl);

    /// <summary>
    /// Locks the account for the supplied duration; subsequent
    /// <see cref="GetLockRemainingAsync"/> calls will return its remaining TTL.
    /// </summary>
    Task LockAsync(string email, TimeSpan ttl);

    /// <summary>
    /// Clears the failure counter (and any active lock) on successful login.
    /// </summary>
    Task ResetAsync(string email);
}
